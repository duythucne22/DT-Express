# 📋 SERVICE-LEVEL Aggregate

## 🎯 Responsibility

> **Single Purpose**: Define **service products** - the contract between business and customer.

Service Level is the "what we promise" entity. It bridges customer expectations with operational capabilities.

---

## 🔗 Relationship Context

```
┌─────────────┐                         ┌─────────────┐
│  CUSTOMER   │                         │   CARRIER   │
│(subscribes) │                         │ (fulfills)  │
└──────┬──────┘                         └──────┬──────┘
       │ N                                     │ N
       │                                       │
       ▼ M                                     ▼ M
┌─────────────────────────────────────────────────────┐
│                   SERVICE-LEVEL                     │
│                   (Aggregate)                       │
└─────────────────────────┬───────────────────────────┘
                          │
       ┌──────────────────┼──────────────────┐
       │                  │                  │
       ▼ 1                ▼ 1                ▼ owns
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│    ORDER    │    │  SHIPMENT   │    │     SLA     │
│ (selected)  │    │ (commits)   │    │ COMMITMENT  │
└─────────────┘    └─────────────┘    │  (owned)    │
                                      └─────────────┘
```

---

## 📋 Core Structure

```
ServiceLevel (Aggregate Root)
│
├── Identity
│   ├── serviceLevelId: ServiceLevelId
│   └── code: string (e.g., "EXPRESS", "SAME_DAY")
│
├── Basic Info
│   ├── name: string
│   └── status: ServiceStatus (ACTIVE | DEPRECATED | INACTIVE)
│
├── Classification
│   ├── category: ServiceCategory (STANDARD | EXPRESS | PREMIUM | FREIGHT)
│   └── priority: int (for routing algorithm weight)
│
├── SLA Commitments (Owned Entities)
│   └── commitments: SLACommitment[]
│       ├── commitmentId: CommitmentId
│       ├── routeType: RouteType (INTRACITY | INTERCITY | CROSS_REGION)
│       ├── maxDeliveryHours: int
│       └── guaranteeType: GuaranteeType (BEST_EFFORT | GUARANTEED | MONEY_BACK)
│
├── Pricing Rules (Owned Value Object)
│   └── pricing: PricingRule
│       ├── basePrice: Money
│       ├── pricePerKg: Money
│       ├── pricePerKm: Money?
│       └── surcharges: Surcharge[]
│
├── Constraints
│   ├── maxWeight: Weight
│   ├── maxDimensions: Dimension
│   ├── acceptsFragile: bool
│
└── Timestamp
    └── createdAt: DateTime
```

---

## 🎭 Service Categories

```
┌───────────────────────────────────────────────────────────────────┐
│                     SERVICE LEVEL SPECTRUM                        │
├───────────────────────────────────────────────────────────────────┤
│   Speed ──────────────────────────────────────────────────► Cost  │
│                                                                   │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐           │
│  │ SAME_DAY │  │ EXPRESS  │  │ STANDARD │  │ ECONOMY  │           │
│  │          │  │          │  │          │  │          │           │
│  │ 4-6 hrs  │  │ 1-2 days │  │ 3-5 days │  │ 5-7 days │           │
│  │ $$$$$    │  │ $$$      │  │ $$       │  │ $        │           │
│  │ PREMIUM  │  │ EXPRESS  │  │ STANDARD │  │ FREIGHT  │           │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘           │
│                                                                   │
│  Routing Priority:  1 (highest)    2           3          4       │
└───────────────────────────────────────────────────────────────────┘
```

---

## 📏 SLA Commitment Examples

```
ServiceLevel: EXPRESS

┌────────────────────────────────────────────────────────────────────┐
│  SLA Commitments by Route Type                                     │
├────────────────────────────────────────────────────────────────────┤
│  RouteType      │ MaxHours │ Guarantee    │ Penalty                │
│  ───────────────┼──────────┼──────────────┼────────────────────    │
│  INTRACITY      │    24    │ GUARANTEED   │ 50% refund if late     │
│  INTERCITY      │    48    │ GUARANTEED   │ 50% refund if late     │
│  CROSS_REGION   │    72    │ BEST_EFFORT  │ None                   │
└────────────────────────────────────────────────────────────────────┘

ServiceLevel: SAME_DAY

┌────────────────────────────────────────────────────────────────────┐
│  RouteType      │ MaxHours │ Guarantee    │ Penalty                │
│  ───────────────┼──────────┼──────────────┼────────────────────    │
│  INTRACITY      │     6    │ MONEY_BACK   │ 100% refund if late    │
│  INTERCITY      │    N/A   │ Not offered  │                        │
└────────────────────────────────────────────────────────────────────┘
```

---

## 🔑 Key Business Rules

### Invariants
```
1. ServiceLevel code MUST be unique
2. ACTIVE service MUST have at least one SLACommitment
3. Pricing basePrice > 0
4. maxWeight and maxDimensions must be positive
5. SAME_DAY services limited to INTRACITY routes
```

### Service Selection Rules
```
ValidateServiceForOrder(order, serviceLevel):
├── Check: serviceLevel.status == ACTIVE
├── Check: order.totalWeight <= serviceLevel.maxWeight
├── Check: order.maxDimension <= serviceLevel.maxDimensions
├── Check: order.containsFragile → serviceLevel.acceptsFragile
├── Check: order.containsHazmat → serviceLevel.acceptsHazmat
└── Check: routeType has valid SLACommitment
```

---

## 🔗 Integration Points

| Connects To | Relationship | Purpose |
|-------------|--------------|---------|
| Customer | N:M | Customers subscribe to services |
| Order | 1:N | Order selects a service level |
| Shipment | 1:N | Shipment bound by service SLA |
| Carrier | N:M | Carriers fulfill service levels |

---

## 💡 Design Decisions

### Why SLA by RouteType?
```
Same service, different promises:

EXPRESS delivery:
- Shanghai → Shanghai (INTRACITY): 24h guaranteed
- Shanghai → Beijing (INTERCITY): 48h guaranteed  
- Shanghai → Urumqi (CROSS_REGION): 72h best-effort

Real-world logistics: distance affects promise
```

### Why Pricing as Value Object?
```
PricingRule = immutable calculation formula

Simple model (covers 80% cases):
totalPrice = basePrice + (weight × pricePerKg) + Σ(surcharges)

Where surcharge can be:
- REMOTE_AREA: +$5
- OVERSIZE: +$10
- FRAGILE_HANDLING: +$3

Extension point: Replace with PricingStrategy pattern
when rules become complex (volume discounts, time-based, etc.)
```

### Why Priority Field?
```
Routing algorithm uses priority:

When multiple routes exist:
- SAME_DAY (priority=1): Always choose fastest path
- EXPRESS (priority=2): Balance speed vs cost
- ECONOMY (priority=4): Choose cheapest path

Priority feeds into Strategy Pattern weight calculation
```

---

## 📊 Service-Carrier Mapping

```
Which carriers can fulfill which services?

┌───────────────────────────────────────────────────────────────────┐
│  Service Level    │ SF Express │ JD Logistics │ ZTO Express       │
│  ─────────────────┼────────────┼──────────────┼────────────────   │
│  SAME_DAY         │     ✅     │      ✅      │       ❌         │
│  EXPRESS          │     ✅     │      ✅      │       ✅         │
│  STANDARD         │     ✅     │      ✅      │       ✅         │
│  ECONOMY          │     ❌     │      ❌      │       ✅         │
└───────────────────────────────────────────────────────────────────┘

Stored in: Carrier.offerings[].serviceLevelId
Used by: Carrier selection algorithm
```

---
