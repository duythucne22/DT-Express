# 📊 DT-Express Data Model Overview

## 🗺️ Aggregate Relationship Map

```
┌────────────────────────────────────────────────────────────────────────────────┐
│                         CORE AGGREGATE RELATIONSHIPS                           │
├────────────────────────────────────────────────────────────────────────────────┤
│                              ┌──────────────────┐                              │
│                              │    CUSTOMER      │                              │
│                              │    AGGREGATE     │                              │
│                              │  (Business Root) │                              │
│                              └────────┬─────────┘                              │
│                                       │ places                                 │
│                                       ▼                                        │
│    ┌──────────────────┐      ┌──────────────────┐      ┌──────────────────┐    │
│    │   SERVICE-LEVEL  │◄─────│      ORDER       │─────►│    WAREHOUSE     │    │
│    │    AGGREGATE     │select│    AGGREGATE     │origin│    AGGREGATE     │    │
│    │  (Product/SLA)   │      │ (Business Event) │      │  (Inventory Hub) │    │
│    └────────┬─────────┘      └────────┬─────────┘      └────────┬─────────┘    │
│             │ defines                 │ creates                 │ connects     │
│             │ capability              ▼                         ▼              │
│             │                ┌──────────────────┐      ┌──────────────────┐    │
│             └───────────────►│    SHIPMENT      │◄─────│   NETWORK-NODE   │    │
│                   matches    │    AGGREGATE     │routes│    AGGREGATE     │    │
│                              │(Physical Journey)│      │ (Logistics Mesh) │    │
│                              └────────┬─────────┘      └──────────────────┘    │
│                                       │ assigned to                            │
│                                       ▼                                        │
│                              ┌──────────────────┐                              │
│                              │     CARRIER      │                              │
│                              │    AGGREGATE     │                              │
│                              │ (Execution Party)│                              │
│                              └──────────────────┘                              │
└────────────────────────────────────────────────────────────────────────────────┘
```

---

## 📦 Aggregate Inventory

### Core Aggregates (Must Have)

| Aggregate | Responsibility | Boundary |
|-----------|---------------|----------|
| [ORDER](ORDER-AGGREGATE.md) | Business intent capture | Order → LineItems |
| [SHIPMENT](SHIPMENT-AGGREGATE.md) | Physical fulfillment tracking | Shipment → Packages → Events |
| [CARRIER](CARRIER-AGGREGATE.md) | External partner capability | Carrier → Services → Rates |
| [CUSTOMER](CUSTOMER-AGGREGATE.md) | Business relationship | Customer → Contracts → Preferences |
| [WAREHOUSE](WAREHOUSE-AGGREGATE.md) | Inventory & dispatch origin | Warehouse → Zones → Capacity |
| [NETWORK-NODE](NETWORK-NODE-AGGREGATE.md) | Logistics topology | Node → Connections → Schedules |
| [SERVICE-LEVEL](SERVICE-LEVEL-AGGREGATE.md) | Product definition & SLA | Service → Rules → Commitments |

### Shared Value Objects

| Value Object | Used By | Purpose |
|--------------|---------|---------|
| `Address` | Order, Customer, Warehouse, Node | Standardized location |
| `GeoCoordinate` | Warehouse, Node, Shipment | GPS positioning |
| `Money` | Order, Service, Carrier | Currency handling |
| `Weight` | Order, Shipment | Mass with unit |
| `Dimension` | Order, Shipment | Volume calculation |
| `TimeWindow` | Service, Order, Node | Delivery/operation windows |

---

## 🔗 Relationship Summary Matrix

```
              │ Customer │ Order │ Shipment │ Carrier │ Warehouse │ Node │ Service │
──────────────┼──────────┼───────┼──────────┼─────────┼───────────┼──────┼─────────┤
Customer      │    -     │  1:N  │    -     │   N:M   │     -     │  -   │   N:M   │
Order         │   N:1    │   -   │   1:N    │    -    │    N:1    │  -   │   N:1   │
Shipment      │    -     │  N:1  │    -     │   N:1   │    N:1    │ N:M  │   N:1   │
Carrier       │   N:M    │   -   │   1:N    │    -    │     -     │ N:M  │   N:M   │
Warehouse     │    -     │  1:N  │   1:N    │    -    │     -     │ N:1  │    -    │
Node          │    -     │   -   │   N:M    │   N:M   │    1:N    │  -   │    -    │
Service       │   N:M    │  1:N  │   1:N    │   N:M   │     -     │  -   │    -    │
```

### Key Relationships Explained

| From → To | Cardinality | Business Meaning |
|-----------|-------------|------------------|
| Customer → Order | 1:N | One customer places many orders |
| Order → Shipment | 1:N | One order may split into multiple shipments |
| Shipment → Carrier | N:1 | Each shipment assigned to one carrier |
| Shipment → Node | N:M | Shipment passes through multiple nodes (route) |
| Warehouse → Node | 1:1 | Warehouse is a special type of node |
| Service → Carrier | N:M | Services fulfilled by multiple carriers |
| Customer → Service | N:M | Customers subscribe to available services |

---

## 🎭 Aggregate Boundaries (DDD Context)

```
┌───────────────────────────────────────────────────────────────────────────┐
│                           BOUNDED CONTEXTS                                │
├───────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                     ORDER MANAGEMENT CONTEXT                        │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │  │
│  │  │   Order     │  │  Customer   │  │  Service    │                  │  │
│  │  │  Aggregate  │  │  Aggregate  │  │  Aggregate  │                  │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                  │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                              │ Domain Event: OrderCreated                 │
│                              ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    FULFILLMENT CONTEXT                              │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │  │
│  │  │  Shipment   │  │  Warehouse  │  │   Carrier   │                  │  │
│  │  │  Aggregate  │  │  Aggregate  │  │  Aggregate  │                  │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                  │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                              │ Domain Event: ShipmentDispatched           │
│                              ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                     NETWORK CONTEXT                                 │  │
│  │  ┌─────────────┐  ┌─────────────┐                                   │  │
│  │  │    Node     │  │    Route    │  (calculated, not persisted)      │  │
│  │  │  Aggregate  │  │   (Value)   │                                   │  │
│  │  └─────────────┘  └─────────────┘                                   │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## 📐 Core Design Rules

### Rule 1: Reference by ID, Not Object
```
Order {
    customerId: CustomerId     ✅ Reference by ID
    customer: Customer         ❌ Direct object reference
}
```

### Rule 2: Aggregate Owns Its Children
```
Shipment (Aggregate Root)
├── Package[]        ← Owned, loaded together
├── TrackingEvent[]  ← Owned, loaded together
└── carrierId        ← Reference only
```

### Rule 3: Cross-Aggregate via Domain Events
```
OrderCreated (Domain Event)
    │
    ├──► ShipmentService.CreateShipment()
    ├──► InventoryService.Reserve()
    └──► NotificationService.Notify()
```

---

## 📚 Document Index

| Document | Focus |
|----------|-------|
| [ORDER-AGGREGATE](ORDER-AGGREGATE.md) | Order lifecycle, line items, state machine |
| [SHIPMENT-AGGREGATE](SHIPMENT-AGGREGATE.md) | Package tracking, events, carrier assignment |
| [CARRIER-AGGREGATE](CARRIER-AGGREGATE.md) | External partner, capabilities, integration |
| [CUSTOMER-AGGREGATE](CUSTOMER-AGGREGATE.md) | Business relationships, contracts, preferences |
| [WAREHOUSE-AGGREGATE](WAREHOUSE-AGGREGATE.md) | Origin points, capacity, inventory |
| [NETWORK-NODE-AGGREGATE](NETWORK-NODE-AGGREGATE.md) | Logistics topology, connections |
| [SERVICE-LEVEL-AGGREGATE](SERVICE-LEVEL-AGGREGATE.md) | Products, SLAs, pricing rules |
| [VALUE-OBJECTS](VALUE-OBJECTS.md) | Shared immutable concepts |

---