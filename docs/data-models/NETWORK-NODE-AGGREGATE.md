# 🌐 NETWORK-NODE Aggregate

## 🎯 Responsibility

> **Single Purpose**: Define the **logistics topology** - the graph of locations through which shipments flow.

Network Node makes routing tangible. It transforms abstract geography into discrete, manageable waypoints.

---

## 🔗 Relationship Context

```
┌─────────────┐                         ┌─────────────┐
│  WAREHOUSE  │                         │   CARRIER   │
│ (is-a node) │                         │ (operates)  │
└──────┬──────┘                         └──────┬──────┘
       │ 1                                     │ N
       │                                       │
       ▼ 1                                     ▼ M
┌─────────────────────────────────────────────────────┐
│                   NETWORK-NODE                      │
│                   (Aggregate)                       │
└─────────────────────────┬───────────────────────────┘
                          │
       ┌──────────────────┼──────────────────┐
       │                  │                  │
       ▼ N                ▼ N:M              ▼ owns
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  SHIPMENT   │    │    NODE     │    │ CONNECTION  │
│ (passes)    │    │ (neighbor)  │    │  (owned)    │
└─────────────┘    └─────────────┘    └─────────────┘
```

---

## 📋 Core Structure

```
NetworkNode (Aggregate Root)
│
├── Identity
│   ├── nodeId: NodeId
│   └── code: string (e.g., "HUB-SHA-PUDONG")
│
├── Location
│   ├── name: string
│   ├── address: Address
│   └── coordinates: GeoCoordinate
│
├── Classification
│   ├── type: NodeType (WAREHOUSE | HUB | SORTING_CENTER | DELIVERY_STATION)
│   ├── tier: NodeTier (NATIONAL | REGIONAL | LOCAL)
│   └── status: NodeStatus (ACTIVE | MAINTENANCE | CLOSED)
│
├── Connections (Owned Entities) - Outbound edges in graph
│   └── connections: NodeConnection[]
│       ├── connectionId: ConnectionId
│       ├── targetNodeId: NodeId
│       ├── distance: Distance (Value Object)
│       ├── transitTime: Duration (typical travel time)
│       ├── transportMode: TransportMode (TRUCK | AIR | RAIL)
│       └── isActive: bool
│
├── Operating Carriers
│   └── carrierIds: CarrierId[] (carriers that service this node)
│
├── Capabilities
│   ├── canOriginateShipment: bool (can be first node)
│   ├── canTerminateShipment: bool (can be last node)
│   └── handlesInternational: bool
│
└── Timestamp
    ├── createdAt: DateTime

```

---

## 🎭 Node Types

```
┌─────────────────────────────────────────────────────────────────────┐
│                        NODE TYPE HIERARCHY                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│                      ┌─────────────────┐                           │
│                      │    NATIONAL     │                           │
│                      │      HUB        │ ← Major transit point     │
│                      │  (Tier: NATIONAL)                           │
│                      └────────┬────────┘                           │
│                               │                                     │
│              ┌────────────────┼────────────────┐                   │
│              │                │                │                    │
│              ▼                ▼                ▼                    │
│     ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │
│     │  SORTING    │  │  REGIONAL   │  │  WAREHOUSE  │             │
│     │   CENTER    │  │    HUB      │  │ (Origin)    │             │
│     │(Tier: REGIONAL)│(Tier: REGIONAL)│(Tier: REGIONAL)            │
│     └──────┬──────┘  └──────┬──────┘  └─────────────┘             │
│            │                │                                       │
│            └────────┬───────┘                                       │
│                     │                                               │
│                     ▼                                               │
│            ┌─────────────────┐                                      │
│            │    DELIVERY     │                                      │
│            │    STATION      │ ← Last-mile dispatch                │
│            │  (Tier: LOCAL)  │                                      │
│            └─────────────────┘                                      │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

| Type | Purpose | Tier |
|------|---------|------|
| `WAREHOUSE` | Origin point, stores inventory | REGIONAL |
| `HUB` | Major consolidation/distribution | NATIONAL/REGIONAL |
| `SORTING_CENTER` | Package sorting, route splitting | REGIONAL |
| `DELIVERY_STATION` | Last-mile dispatch | LOCAL |

---

## 🔗 Connection Graph Model

```
Example: Shanghai to Beijing Route

┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│  [WH-SHA-01]──────►[HUB-SHA-PUDONG]──────►[HUB-BEI-CAPITAL]        │
│   WAREHOUSE         NATIONAL HUB           NATIONAL HUB             │
│                     │                      │                        │
│                     │ 2hrs, TRUCK          │ 2.5hrs, AIR            │
│                     │ 50km                 │ 1200km                 │
│                     ▼                      ▼                        │
│                                    ───────►[DS-BEI-CHAOYANG]       │
│                                             DELIVERY STATION        │
│                                             │                       │
│                                             │ 1hr, TRUCK            │
│                                             │ 15km                  │
│                                             ▼                       │
│                                            📍 Destination           │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

NodeConnection represents each arrow:
{
    targetNodeId: "HUB-SHA-PUDONG",
    distance: { value: 50, unit: KM },
    transitTime: { hours: 2 },
    transportMode: TRUCK
}
```

---

## 🔑 Key Business Rules

### Invariants
```
1. Node code MUST be unique
2. ACTIVE node MUST have valid coordinates
3. Connections must reference existing nodes
4. At least one carrier must service active node
5. WAREHOUSE type implies canOriginateShipment = true
6. DELIVERY_STATION implies canTerminateShipment = true
```

### Graph Integrity Rules
```
Routing Algorithm Requirements:
├── Graph must be connected (path exists between any two nodes)
├── No self-loops (node cannot connect to itself)
├── TransitTime > 0 for all connections
└── At least one path exists: any WAREHOUSE → any DELIVERY_STATION
```

---

## 🔗 Integration Points

| Connects To | Relationship | Purpose |
|-------------|--------------|---------|
| Warehouse | 1:1 | Warehouse is a specialized node |
| Shipment | N:M | Shipment routes through nodes |
| Carrier | N:M | Carriers operate at nodes |

---

## 💡 Design Decisions

### Why Graph Model?
```
Routing = Graph Traversal Problem

Dijkstra/A* algorithms need:
- Nodes (vertices)
- Connections (edges with weights)
- Weight = distance, time, or cost

NetworkNode + Connections = Complete graph definition
```

### Why Connection is Owned Entity?
```
Connection lifecycle = Node lifecycle
- Delete node → delete its connections
- No independent business meaning

Alternative (rejected): Connection as separate aggregate
- Adds complexity
- Cross-aggregate transaction needed
- Overkill for this domain
```

### Why CarrierIds on Node?
```
Not all carriers serve all locations:

HUB-SHA-PUDONG:
└── carrierIds: [SF, JD, ZTO, YTO]  ← Major hub, many carriers

DS-RURAL-VILLAGE:
└── carrierIds: [LOCAL_COURIER]     ← Remote, limited service

Enables: Carrier filtering in route calculation
```

---

## 📊 Route Calculation Support

```
Input: Origin Node, Destination Node, ServiceLevel
Output: Optimal path through network

┌─────────────────────────────────────────────────────────────────────┐
│  Strategy Pattern Selection (from 01-DYNAMIC-ROUTING)              │
│                                                                     │
│  ServiceLevel: EXPRESS                                              │
│  Selected Strategy: TimeOptimizedStrategy                           │
│                                                                     │
│  Graph traversal weights: connection.transitTime                    │
│  Result: [WH-SHA, HUB-SHA, HUB-BEI, DS-BEI]                        │
│  Total Time: 5.5 hours                                              │
│                                                                     │
│  ServiceLevel: ECONOMY                                              │
│  Selected Strategy: CostOptimizedStrategy                           │
│                                                                     │
│  Graph traversal weights: connection.cost (derived)                 │
│  Result: [WH-SHA, SORT-NANJING, HUB-BEI, DS-BEI]                   │
│  Total Time: 18 hours (but cheaper)                                 │
└─────────────────────────────────────────────────────────────────────┘
```

---
