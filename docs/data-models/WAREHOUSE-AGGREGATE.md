# 🏭 WAREHOUSE Aggregate

## 🎯 Responsibility

> **Single Purpose**: Manage **origin points** for shipments - inventory locations and dispatch capabilities.

Warehouse is where physical fulfillment begins. It's the decision point for "where to ship from."

---

## 🔗 Relationship Context

```
┌─────────────┐                         ┌─────────────┐
│    ORDER    │                         │ NETWORK     │
│ (origin)    │                         │    NODE     │
└──────┬──────┘                         └──────┬──────┘
       │ N                                     │ 1
       │                                       │
       ▼ 1                                     ▼ 1
┌─────────────────────────────────────────────────────┐
│                    WAREHOUSE                        │
│                   (Aggregate)                       │
└─────────────────────────┬───────────────────────────┘
                          │
       ┌──────────────────┴──────────────────┐
       │                                     │
       ▼ N                                   ▼ 1
┌─────────────┐                       ┌─────────────┐
│  SHIPMENT   │                       │   CAPACITY  │
│ (dispatched)│                       │   CONFIG    │
└─────────────┘                       │  (owned)    │
                                      └─────────────┘
```

---

## 📋 Core Structure

```
Warehouse (Aggregate Root)
│
├── Identity
│   ├── warehouseId: WarehouseId
│   ├── code: string (e.g., "WH-SHA-01")
│   └── nodeId: NodeId (links to network topology)
│
├── Location
│   ├── name: string
│   ├── address: Address
│   ├── coordinates: GeoCoordinate
│   └── timezone: string
│
├── Classification
│   ├── type: WarehouseType (FULFILLMENT | DISTRIBUTION | CROSS_DOCK)
│   └── status: WarehouseStatus (ACTIVE | MAINTENANCE | CLOSED)
│
├── Operational Config
│   ├── operatingHours: OperatingHours (Value Object)
│   │   ├── mondayToFriday: TimeRange
│   │   ├── saturday: TimeRange?
│   │   └── sunday: TimeRange?
│   └── cutoffTime: Time (last order acceptance)
│
├── Capacity (Owned Entity)
│   └── capacity: WarehouseCapacity
│       ├── maxDailyShipments: int
│       ├── currentDailyLoad: int (no store , flucuated, read model)
│       └── utilizationPercent: decimal (calculated, no store,= currentload / capacity) 
│
├── Service Coverage
│   └── servicedRegions: RegionCode[] (areas this warehouse serves)
│
└── Timestamps
    └── createdAt: DateTime
```

---

## 🎭 Warehouse Types

```
┌───────────────────────────────────────────────────────────────────┐
│                      WAREHOUSE TYPE ROLES                         │
├───────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐  │
│  │   FULFILLMENT   │   │  DISTRIBUTION   │   │   CROSS_DOCK    │  │
│  │                 │   │                 │   │                 │  │
│  │ • Stores goods  │   │ • Regional hub  │   │ • No storage    │  │
│  │ • Pick & pack   │   │ • Bulk break    │   │ • Transfer only │  │
│  │ • Order origin  │   │ • Consolidation │   │ • Fast turnover │  │
│  │                 │   │                 │   │                 │  │
│  │ Primary for:    │   │ Primary for:    │   │ Primary for:    │  │
│  │ B2C orders      │   │ B2B shipments   │   │ Express transit │  │
│  └─────────────────┘   └─────────────────┘   └─────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
```

---

## 🔑 Key Business Rules

### Invariants
```
1. Warehouse code MUST be unique
2. ACTIVE warehouse MUST have valid address and coordinates
3. nodeId MUST reference existing NetworkNode
4. currentDailyLoad <= maxDailyShipments
5. cutoffTime must be within operatingHours
```

### Capacity Rules
```
CanAcceptOrder(order):
├── Check: warehouse.status == ACTIVE
├── Check: currentDailyLoad < maxDailyShipments
├── Check: currentTime < cutoffTime
├── Check: order.destination.region IN servicedRegions
└── Check: today is operating day
```

---

## 🔗 Integration Points

| Connects To | Relationship | Purpose |
|-------------|--------------|---------|
| Order | 1:N | Warehouse is origin for orders |
| Shipment | 1:N | Shipments dispatched from warehouse |
| NetworkNode | 1:1 | Warehouse is a node in logistics network |

---

## 💡 Design Decisions

### Why Warehouse has NodeId?
```
Warehouse IS-A NetworkNode (specialized type)

NetworkNode (generic)           Warehouse (specific)
├── nodeId                      ├── warehouseId
├── location                    ├── code
├── type: WAREHOUSE             ├── nodeId ────────► links to parent
└── connections                 ├── inventory logic
                                └── capacity logic

Benefits:
- Routing algorithms work with nodes (including warehouses)
- Warehouse adds fulfillment-specific behavior
- Clean separation: topology vs operations
```

### Why ServicedRegions?
```
Order allocation logic:
"Which warehouse should fulfill this order?"

┌─────────────────────────────────────────────────────┐
│  Order destination: Shanghai Pudong                 │
│                                                     │
│  Candidate Warehouses:                              │
│  ├── WH-SHA-01 (Shanghai) → servicedRegions: [SHA]  │ ✅
│  ├── WH-BEI-01 (Beijing)  → servicedRegions: [BEI]  │ ❌
│  └── WH-HAN-01 (Hangzhou) → servicedRegions: [HAN, SHA] │ ✅
│                                                     │
│  Selection: WH-SHA-01 (closest in same region)      │
└─────────────────────────────────────────────────────┘
```

### Why Daily Capacity Model?
```
Simple but effective for core scenarios:
- maxDailyShipments = operational limit
- currentDailyLoad = today's assigned orders
- Reset daily at midnight (timezone-aware)

Extension point for future:
- Hourly capacity slots
- SKU-specific capacity
- Dynamic capacity based on staffing
```

---

## 📊 Capacity Utilization Example

```
Warehouse: WH-SHA-01 (Shanghai Fulfillment Center)

┌─────────────────────────────────────────────────────┐
│  Daily Capacity: 1000 shipments                     │
│  Current Load:   750 shipments                      │
│  Utilization:    75%                                │
│                                                     │
│  ████████████████████████░░░░░░░░ 75%               │
│                                                     │
│  Status: ACCEPTING ORDERS                           │
│  Cutoff: 18:00 CST                                  │
└─────────────────────────────────────────────────────┘

When utilization > 90%:
→ Overflow to next nearest warehouse
→ Alert operations team
```

---