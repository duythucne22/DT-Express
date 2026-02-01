# 📦 ORDER Aggregate

## 🎯 Responsibility

> **Single Purpose**: Capture and manage the **business intent** of a delivery request.

The Order is the **entry point** of the system - it represents what the customer wants, not how it will be fulfilled.

---

## 🔗 Relationship Context

```
                    ┌─────────────┐
                    │  CUSTOMER   │
                    │ (owns)      │
                    └──────┬──────┘
                           │ 1
                           │
                           ▼ N
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  SERVICE    │◄────│    ORDER    │────►│  WAREHOUSE  │
│   LEVEL     │ 1   │ (Aggregate) │   1 │  (origin)   │
│ (selected)  │     └──────┬──────┘     └─────────────┘
└─────────────┘            │
                           │ 1
                           │
                           ▼ N
                    ┌─────────────┐
                    │  SHIPMENT   │
                    │ (fulfills)  │
                    └─────────────┘
```

---

## 📋 Core Structure

```
Order (Aggregate Root)
│
├── Identity
│   └── orderId: OrderId (unique)
│
├── Relationships (by ID reference)
│   ├── customerId: CustomerId
│   ├── serviceLevelId: ServiceLevelId
│   └── originWarehouseId: WarehouseId
│
├── Core Data
│   ├── destination: Address (Value Object)
│   ├── requestedDeliveryWindow: TimeWindow
│   └── specialInstructions: string?
│
├── Line Items (Owned Entities)
│   └── items: OrderItem[]
│       ├── itemId: OrderItemId
│       ├── description: string
│       ├── quantity: int
│       ├── weight: Weight
│       └── dimensions: Dimension
│
├── State
│   ├── status: OrderStatus (enum)
│   └── statusHistory: StatusChange[]
│
└── Timestamps
    └── CreatedAt: DateTime
```

---

## 🎭 State Machine

```
┌────────────────────────────────────────────────────────────────────┐
│                        ORDER STATE FLOW                            │
├────────────────────────────────────────────────────────────────────┤
│    ┌─────────┐     ┌─────────┐     ┌─────────┐     ┌──────────┐    │
│    │ PENDING │────►│CONFIRMED│────►│ALLOCATED│────►│DISPATCHED│    │
│    └────┬────┘     └────┬────┘     └────┬────┘     └────┬─────┘    │
│         │               │               │               │          │
│         │               │               │               ▼          │
│         │               │               │          ┌─────────┐     │
│         │               │               │          │DELIVERED│     │
│         │               │               │          └─────────┘     │
│         │               │               │                          │
│         ▼               ▼               ▼                          │
│    ┌─────────────────────────────────────────┐                     │
│    │              CANCELLED                  │                     │
│    └─────────────────────────────────────────┘                     │
└────────────────────────────────────────────────────────────────────┘
```

| Status | Meaning | Allowed Transitions |
|--------|---------|---------------------|
| `PENDING` | Order received, awaiting validation | CONFIRMED, CANCELLED |
| `CONFIRMED` | Validated, ready for fulfillment | ALLOCATED, CANCELLED |
| `ALLOCATED` | Warehouse & carrier assigned | DISPATCHED, CANCELLED |
| `DISPATCHED` | Shipment created and handed off | DELIVERED |
| `DELIVERED` | Successfully completed | (terminal) |
| `CANCELLED` | Order cancelled | (terminal) |

---

## 🔑 Key Business Rules

### Invariants (Always True)
```
1. Order MUST have at least one OrderItem
2. Order MUST reference valid CustomerId
3. Order MUST reference valid ServiceLevelId
4. Total weight = SUM(items.weight × items.quantity)
5. Status transitions follow state machine
```

### Domain Events Emitted
```
OrderCreated        → triggers warehouse allocation
OrderConfirmed      → triggers route calculation
OrderAllocated      → triggers shipment creation
OrderDispatched     → triggers customer notification
OrderDelivered      → triggers billing
OrderCancelled      → triggers refund process
```

---

## 🔗 Integration Points

| Connects To | Relationship | Purpose |
|-------------|--------------|---------|
| Customer | N:1 | Ownership, billing, preferences |
| ServiceLevel | N:1 | SLA commitments, pricing |
| Warehouse | N:1 | Origin point for fulfillment |
| Shipment | 1:N | Physical execution of order |

---

## 💡 Design Decisions

### Why Order ≠ Shipment?
```
Order = WHAT customer wants (business)
Shipment = HOW we deliver it (operations)

One Order → Multiple Shipments (split delivery)
```

### Why Status History?
```
Audit requirement: Track all state changes
Each StatusChange = {
    from: OrderStatus
    to: OrderStatus
    changedAt: DateTime
    changedBy: UserId
    reason: string?
}
```

---