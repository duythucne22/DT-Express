# 🚚 SHIPMENT Aggregate

## 🎯 Responsibility

> **Single Purpose**: Track the **physical journey** of goods from origin to destination.

Shipment represents the operational reality - what's actually moving, where it is, and who's carrying it.

---

## 🔗 Relationship Context

```
┌─────────────┐                         ┌─────────────┐
│    ORDER    │                         │   CARRIER   │
│ (source)    │                         │ (executor)  │
└──────┬──────┘                         └──────┬──────┘
       │ 1                                     │ 1
       │                                       │
       ▼ N                                     ▼ N
┌─────────────────────────────────────────────────────┐
│                    SHIPMENT                         │
│                   (Aggregate)                       │
└─────────────────────────┬───────────────────────────┘
                          │
       ┌──────────────────┼──────────────────┐
       │                  │                  │
       ▼ N                ▼ N                ▼ N:M
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   PACKAGE   │    │   TRACKING  │    │    NODE     │
│  (owned)    │    │    EVENT    │    │  (route)    │
└─────────────┘    │  (owned)    │    └─────────────┘
                   └─────────────┘
```

---

## 📋 Core Structure

```
Shipment (Aggregate Root)
│
├── Identity
│   ├── shipmentId: ShipmentId
│   └── trackingNumber: string (carrier-assigned)
│
├── Relationships (by ID reference)
│   ├── orderId: OrderId
│   ├── carrierId: CarrierId
│   ├── serviceLevelId: ServiceLevelId
│   ├── originNodeId: NodeId (warehouse)
│   └── destinationNodeId: NodeId
│
├── Route (Planned Path)
│   └── plannedRoute: NodeId[] (sequence of nodes)
│
├── Packages (Owned Entities)
│   └── packages: Package[]
│       ├── packageId: PackageId
│       ├── weight: Weight
│       ├── dimensions: Dimension
│       └── contents: string
│
├── Tracking Events (Owned Entities)
│   └── events: TrackingEvent[]
│       ├── eventId: EventId
│       ├── eventType: TrackingEventType
│       ├── occurredAt: DateTime
│       ├── nodeId: NodeId?
│       ├── location: GeoCoordinate?
│       └── description: string
│
├── State
│   ├── status: ShipmentStatus
│   └── currentLocation: GeoCoordinate?
│
└── Timestamps
    ├── estimatedDelivery: DateTime
    ├── actualDelivery: DateTime?
    └── createdAt: DateTime
```

---

## 🎭 State Machine

```
┌───────────────────────────────────────────────────────────────────┐
│                     SHIPMENT STATE FLOW                           │
├───────────────────────────────────────────────────────────────────┤
│  ┌────────┐   ┌────────┐   ┌────────┐   ┌────────┐   ┌─────────┐  │
│  │CREATED │──►│PICKED  │──►│IN      │──►│OUT FOR │──►│DELIVERED│  │
│  │        │   │UP      │   │TRANSIT │   │DELIVERY│   │         │  │
│  └───┬────┘   └───┬────┘   └───┬────┘   └───┬────┘   └─────────┘  │
│      │            │            │            │                     │
│      ▼            ▼            ▼            ▼                     │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                      EXCEPTION                              │  │
│  │  (DELAYED | DAMAGED | LOST | RETURNED | HELD)               │  │
│  └─────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
```

| Status | Meaning |
|--------|---------|
| `CREATED` | Shipment record created, awaiting pickup |
| `PICKED_UP` | Carrier has collected packages |
| `IN_TRANSIT` | Moving through network |
| `OUT_FOR_DELIVERY` | Final mile, on delivery vehicle |
| `DELIVERED` | Successfully handed to recipient |
| `EXCEPTION` | Problem occurred (sub-states exist) |

---

## 📍 Tracking Event Types

```
TrackingEventType (enum)
│
├── Movement Events
│   ├── PICKED_UP
│   ├── DEPARTED_FACILITY
│   ├── ARRIVED_FACILITY
│   ├── OUT_FOR_DELIVERY
│   └── DELIVERED
│
├── Exception Events
│   ├── DELIVERY_ATTEMPTED
│   ├── HELD_AT_FACILITY
│   ├── DELAYED
│   └── RETURNED_TO_SENDER
│
└── Information Events
    ├── CUSTOMS_CLEARED
    ├── SIGNATURE_OBTAINED
    └── PROOF_OF_DELIVERY
```

---

## 🔑 Key Business Rules

### Invariants
```
1. Shipment MUST reference valid OrderId
2. Shipment MUST have at least one Package
3. Shipment MUST have CarrierId once PICKED_UP
4. TrackingEvents are append-only (immutable history)
5. PlannedRoute nodes must be connected in NetworkNode graph
```

### Domain Events Emitted
```
ShipmentCreated         → notifies order, reserves carrier
ShipmentPickedUp        → starts transit tracking
ShipmentArrivedAtNode   → updates ETA, triggers geofence
ShipmentOutForDelivery  → notifies customer (final mile)
ShipmentDelivered       → completes order, triggers billing
ShipmentException       → alerts operations, customer
```

---

## 🔗 Integration Points

| Connects To | Relationship | Purpose |
|-------------|--------------|---------|
| Order | N:1 | Source business intent |
| Carrier | N:1 | Execution partner |
| ServiceLevel | N:1 | SLA for this shipment |
| NetworkNode | N:M | Route through network |

---

## 💡 Design Decisions

### Why Planned Route as Node IDs?
```
PlannedRoute: [Node_A, Node_B, Node_C, Node_D]
                 │        │        │        │
            Warehouse → Hub1 → Hub2 → Destination

- Enables ETA calculation per segment
- Supports geofence alerts at each node
- Allows route deviation detection
```

### Why TrackingEvent is Event-Sourced Style?
```
Events are IMMUTABLE facts:
- Never update, only append
- Full history preserved
- Enables replay and audit
- Each event = observer notification trigger
```

---
