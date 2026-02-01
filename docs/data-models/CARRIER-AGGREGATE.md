# 🏢 CARRIER Aggregate

## 🎯 Responsibility

> **Single Purpose**: Represent **external logistics partners** and their capabilities.

Carrier encapsulates what each logistics provider can do, enabling the Adapter Pattern to integrate with their APIs.

---

## 🔗 Relationship Context

```
┌─────────────┐                         ┌─────────────┐
│  CUSTOMER   │                         │   SERVICE   │
│ (contracts) │                         │    LEVEL    │
└──────┬──────┘                         └──────┬──────┘
       │ N                                     │ N
       │                                       │
       ▼ M                                     ▼ M
┌─────────────────────────────────────────────────────┐
│                     CARRIER                         │
│                   (Aggregate)                       │
└─────────────────────────┬───────────────────────────┘
                          │
       ┌──────────────────┼──────────────────┐
       │                  │                  │
       ▼ 1                ▼ N                ▼ N:M
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   SHIPMENT  │    │   SERVICE   │    │    NODE     │
│ (assigned)  │    │  OFFERING   │    │ (coverage)  │
└─────────────┘    │  (owned)    │    └─────────────┘
                   └─────────────┘
```

---

## 📋 Core Structure

```
Carrier (Aggregate Root)
│
├── Identity
│   ├── carrierId: CarrierId
│   └── code: string (e.g., "SF", "JD", "ZTO")
│
├── Basic Info
│   ├── name: string
│   ├── type: CarrierType (EXPRESS | FREIGHT | LAST_MILE)
│   └── status: CarrierStatus (ACTIVE | SUSPENDED | INACTIVE)
│
├── Integration Config
│   ├── apiEndpoint: string
│   ├── authType: AuthType (API_KEY | OAUTH | CERTIFICATE)
│   └── adapterType: string (maps to Adapter class)
│
├── Service Offerings (Owned Entities)
│   └── offerings: ServiceOffering[]
│       ├── offeringId: OfferingId
│       ├── serviceLevelId: ServiceLevelId (maps to our service)
│       ├── carrierServiceCode: string (carrier's internal code)
│       └── isActive: bool
│
├── Coverage
│   └── coveredNodeIds: NodeId[] (where they operate)
│
├── Performance Metrics (Value Object)
│   └── metrics: CarrierMetrics
│       ├── onTimeDeliveryRate: decimal
│       ├── damageRate: decimal
│       └── avgTransitDays: decimal
│
└── Timestamps
    ├── createdAt: DateTime
```

---

## 🎭 Carrier Types

```
┌───────────────────────────────────────────────────────────────────┐
│                      CARRIER TYPE HIERARCHY                       │
├───────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐  │
│  │     EXPRESS     │   │     FREIGHT     │   │    LAST_MILE    │  │
│  │                 │   │                 │   │                 │  │
│  │ • SF Express    │   │ • Deppon        │   │ • Local Courier │  │
│  │ • JD Logistics  │   │ • ANE Logistics │   │ • FlashEx       │  │
│  │ • ZTO Express   │   │ • YTO Freight   │   │ • Same-day      │  │
│  │                 │   │                 │   │                 │  │
│  │ Fast, tracked   │   │ Bulk, slower    │   │ Urban, fastest  │  │
│  │ B2C focus       │   │ B2B focus       │   │ Hyperlocal      │  │
│  └─────────────────┘   └─────────────────┘   └─────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
```

---

## 🔌 Integration Mapping

```
Adapter Pattern Connection:

Carrier Aggregate              Adapter Implementation
─────────────────              ─────────────────────
carrierId: "SF"       ───────► SFExpressAdapter
adapterType: "SF"              │
apiEndpoint: "..."             ├── CreateShipment()
authType: API_KEY              ├── GetTrackingInfo()
                               ├── CancelShipment()
                               └── GetRates()

Factory Pattern:
CarrierAdapterFactory.Create(carrier.adapterType)
    → returns ICarrierAdapter implementation
```

---

## 🔑 Key Business Rules

### Invariants
```
1. Carrier code MUST be unique
2. ACTIVE carrier MUST have valid apiEndpoint
3. ServiceOffering must map to valid ServiceLevelId
4. Coverage nodes must exist in NetworkNode
```

### Capability Queries
```
CanFulfill(shipment):
├── Check: carrier.status == ACTIVE
├── Check: shipment.originNode IN carrier.coveredNodes
├── Check: shipment.destNode IN carrier.coveredNodes
└── Check: shipment.serviceLevelId IN carrier.offerings
```

---

## 🔗 Integration Points

| Connects To | Relationship | Purpose |
|-------------|--------------|---------|
| Shipment | 1:N | Carrier executes shipments |
| ServiceLevel | N:M | Carrier offers multiple service levels |
| NetworkNode | N:M | Carrier operates in certain areas |
| Customer | N:M | Contracts may specify preferred carriers |

---

## 💡 Design Decisions

### Why ServiceOffering as Owned Entity?
```
Our ServiceLevel ≠ Carrier's Service Code

Example:
┌─────────────────┬──────────────────────┬──────────────────┐
│  Our Service    │  SF Service Code     │  JD Service Code │
├─────────────────┼──────────────────────┼──────────────────┤
│  STANDARD       │  "SF_STANDARD"       │  "JD_ECONOMY"    │
│  EXPRESS        │  "SF_AIR"            │  "JD_EXPRESS"    │
│  SAME_DAY       │  "SF_SAME_DAY"       │  "JD_HOUR"       │
└─────────────────┴──────────────────────┴──────────────────┘

ServiceOffering bridges this mapping per carrier.
```

### Why Metrics in Aggregate?
```
Carrier selection algorithms need:
- Historical performance data
- Real-time capability assessment

Updated periodically (not per transaction)
Used by Strategy pattern for routing decisions
```

---
