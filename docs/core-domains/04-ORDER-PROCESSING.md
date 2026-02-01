# 📦 04-ORDER-PROCESSING - Design Specification

> **Domain**: Order Management System (OMS)  
> **Primary Patterns**: State Pattern (状态模式) + CQRS Pattern (命令查询分离)  
> **Pattern Study Guides**: [STATE-PATTERN.md](../design-patterns/STATE-PATTERN.md) | [CQRS-PATTERN.md](../design-patterns/CQRS-PATTERN.md)  
> **Status**: ⬜ Not Started  
> **Dependencies**: [01-DYNAMIC-ROUTING](01-DYNAMIC-ROUTING.md), [02-MULTI-CARRIER](02-MULTI-CARRIER.md), [03-REALTIME-TRACKING](03-REALTIME-TRACKING.md)

---

## 📋 Table of Contents

1. [Domain Overview](#domain-overview)
2. [Business Context](#business-context)
3. [Feature Specification](#feature-specification)
4. [State Machine Design](#state-machine-design)
5. [Enhanced State Machine - Reverse Logistics](#enhanced-state-machine---reverse-logistics)
6. [CQRS Architecture](#cqrs-architecture)
7. [Deep CQRS - Read/Write Separation](#deep-cqrs---readwrite-separation)
8. [Smart Dispatch Algorithm](#smart-dispatch-algorithm)
9. [Interface Contracts](#interface-contracts)
10. [Command & Query Catalog](#command--query-catalog)
11. [Data Models](#data-models)
12. [Integration Points](#integration-points)
13. [Chinese Industry Practices](#chinese-industry-practices)
14. [Design Pattern Ledger](#design-pattern-ledger)
15. [Study Resources](#study-resources)
16. [Acceptance Criteria](#acceptance-criteria)

---

## 🎯 Domain Overview

### Purpose
The Order Processing domain is the **orchestration center** of the logistics system, managing the complete lifecycle of orders from creation to delivery, coordinating with routing, carrier, and tracking domains.

### Scope
| In Scope | Out of Scope |
|----------|--------------|
| Order creation (multi-channel) | Route calculation (→ 01-DYNAMIC-ROUTING) |
| Order validation | Carrier booking (→ 02-MULTI-CARRIER) |
| Order state management | GPS tracking (→ 03-REALTIME-TRACKING) |
| Smart order dispatch | Warehouse operations (WMS) |
| Order cancellation/modification | Financial settlement |
| Exception handling workflow | Customer management (CRM) |

### Business Value
- **Processing Efficiency**: 45% improvement in order throughput
- **Response Time**: From 800ms to 120ms with CQRS
- **Error Reduction**: State machine prevents invalid transitions
- **Visibility**: Complete order lifecycle tracking
- **Flexibility**: Support multiple order types and channels

---

## 💼 Business Context

### Order Types

| Order Type | Source | Characteristics | Processing |
|------------|--------|-----------------|------------|
| E-commerce | API/Web | High volume, standard | Auto-dispatch |
| B2B | EDI/API | Large, scheduled | Batch processing |
| Walk-in | Counter | Immediate, small | Priority queue |
| Return | App/Web | Reverse logistics | Special handling |
| International | API | Customs required | Multi-leg |

### Business Rules

| Rule ID | Rule Description | Implementation |
|---------|------------------|----------------|
| BR-OR-001 | Order must have valid addresses | Address validation service |
| BR-OR-002 | Order cannot exceed max weight (30kg standard) | Validation rule |
| BR-OR-003 | Express orders dispatch within 30 minutes | SLA monitoring |
| BR-OR-004 | Order can only cancel before pickup | State machine guard |
| BR-OR-005 | Duplicate orders rejected within 5 minutes | Idempotency check |
| BR-OR-006 | COD orders require customer verification | Workflow step |
| BR-OR-007 | Orders auto-cancel after 24h without payment | Scheduled job |

### Use Cases

#### UC-OR-001: Create Order
```
Actor: Customer (Web/API) or Staff (Counter)
Precondition: User authenticated, valid shipping details
Flow:
  1. User submits order request
  2. System validates addresses (external service)
  3. System checks for duplicates
  4. System creates order in CREATED state
  5. System publishes OrderCreated event
  6. System returns order confirmation
Postcondition: Order exists, ready for dispatch
```

#### UC-OR-002: Dispatch Order
```
Actor: System (auto) or Dispatcher (manual)
Precondition: Order in CONFIRMED state, payment completed
Flow:
  1. System requests route calculation (→ 01-DYNAMIC-ROUTING)
  2. System requests carrier quotes (→ 02-MULTI-CARRIER)
  3. System selects optimal carrier
  4. System books shipment with carrier
  5. System transitions order to DISPATCHED
  6. System publishes OrderDispatched event
Postcondition: Shipment booked, tracking number assigned
```

#### UC-OR-003: Track Order
```
Actor: Customer or Staff
Precondition: Order exists with tracking number
Flow:
  1. User requests order status
  2. System retrieves order details (Query)
  3. System fetches real-time tracking (→ 03-REALTIME-TRACKING)
  4. System returns combined status
Postcondition: User sees current order and shipment status
```

#### UC-OR-004: Cancel Order
```
Actor: Customer or Staff
Precondition: Order not yet picked up
Flow:
  1. User requests cancellation with reason
  2. System validates cancellation allowed (state check)
  3. If shipment booked: cancel with carrier
  4. System transitions to CANCELLED state
  5. System publishes OrderCancelled event
  6. If paid: trigger refund process
Postcondition: Order cancelled, resources released
```

---

## 📝 Feature Specification

### Feature Matrix

| Feature ID | Feature Name | Description | Pattern | Priority |
|------------|--------------|-------------|---------|----------|
| OR-F001 | Order Creation | Create via API/Web/Import | Factory | 🔴 High |
| OR-F002 | Order Validation | Validate addresses, weights | Specification | 🔴 High |
| OR-F003 | Duplicate Detection | Prevent duplicate orders | Idempotency | 🔴 High |
| OR-F004 | State Management | Order lifecycle control | State | 🔴 High |
| OR-F005 | Auto Dispatch | Smart carrier selection | Strategy | 🔴 High |
| OR-F006 | Manual Dispatch | Dispatcher override | Command | 🟡 Medium |
| OR-F007 | Order Modification | Change address/items | Command | 🟡 Medium |
| OR-F008 | Order Cancellation | Cancel with validation | Command | 🔴 High |
| OR-F009 | Exception Handling | Handle problems | Chain | 🔴 High |
| OR-F010 | Order Query | Fast order lookup | Query | 🔴 High |
| OR-F011 | Order History | Audit trail | Event Sourcing | 🟡 Medium |
| OR-F012 | Batch Import | Excel/CSV import | Template | 🟢 Low |

### OR-F001: Order Creation

**Description**: Support multiple channels for order creation.

**Channels**:
| Channel | Input Format | Validation Level | SLA |
|---------|--------------|------------------|-----|
| REST API | JSON | Full | < 500ms |
| Web Form | Form data | Full | < 1s |
| Mobile App | JSON | Full | < 500ms |
| Excel Import | XLSX | Batch | < 30s/100 orders |
| EDI | X12/EDIFACT | Mapping + Full | < 5s |

### OR-F004: State Management

**Description**: Control order lifecycle through well-defined states.

**State Transition Rules**:
| From State | To State | Trigger | Guard Condition |
|------------|----------|---------|-----------------|
| CREATED | CONFIRMED | Payment received | Payment valid |
| CREATED | CANCELLED | User cancels | Within cancel window |
| CONFIRMED | DISPATCHED | Carrier booked | Shipment created |
| DISPATCHED | IN_TRANSIT | Carrier pickup | Tracking updated |
| IN_TRANSIT | OUT_FOR_DELIVERY | Last mile start | Driver assigned |
| OUT_FOR_DELIVERY | DELIVERED | Delivery confirmed | Signature/photo |
| OUT_FOR_DELIVERY | FAILED_DELIVERY | Delivery failed | Exception logged |
| FAILED_DELIVERY | OUT_FOR_DELIVERY | Retry scheduled | Within retry limit |
| * | CANCELLED | Cancellation | State allows cancel |
| * | EXCEPTION | Problem detected | Exception triggered |

---

## 🎨 State Machine Design

### Order State Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                     ORDER STATE MACHINE                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│                         ┌─────────┐                                 │
│                         │ CREATED │                                 │
│                         └────┬────┘                                 │
│                   payment    │     cancel                           │
│                   received   │     requested                        │
│                    ┌─────────┴─────────┐                           │
│                    ▼                   ▼                            │
│              ┌───────────┐       ┌───────────┐                     │
│              │ CONFIRMED │       │ CANCELLED │                     │
│              └─────┬─────┘       └───────────┘                     │
│                    │ carrier                 ▲                      │
│                    │ booked                  │ cancel               │
│                    ▼                         │ (if allowed)         │
│              ┌────────────┐                  │                      │
│              │ DISPATCHED │──────────────────┤                      │
│              └─────┬──────┘                  │                      │
│                    │ carrier                 │                      │
│                    │ pickup                  │                      │
│                    ▼                         │                      │
│              ┌────────────┐                  │                      │
│              │ IN_TRANSIT │──────────────────┤                      │
│              └─────┬──────┘                  │                      │
│                    │ out for                 │                      │
│                    │ delivery                │                      │
│                    ▼                                                │
│         ┌──────────────────────┐                                   │
│         │  OUT_FOR_DELIVERY    │                                   │
│         └──────────┬───────────┘                                   │
│           success  │     failure                                    │
│           ┌────────┴────────┐                                      │
│           ▼                 ▼                                       │
│     ┌───────────┐    ┌────────────────┐                            │
│     │ DELIVERED │    │ FAILED_DELIVERY│◄──┐                        │
│     │  (Final)  │    └───────┬────────┘   │ retry                  │
│     └───────────┘            │            │                        │
│                              └────────────┘                        │
│                                                                     │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │ EXCEPTION state can be entered from any state when         │    │
│  │ a problem is detected (address error, carrier issue, etc.) │    │
│  └────────────────────────────────────────────────────────────┘    │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### State Pattern Structure

```
┌─────────────────────────────────────────────────────────────────────┐
│                      STATE PATTERN                                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                    Order (Context)                           │    │
│  │  ──────────────────────────────────────────────────────────  │    │
│  │  - state: IOrderState                                        │    │
│  │  - orderData: OrderData                                      │    │
│  │                                                              │    │
│  │  + Confirm()      → delegates to state.Confirm(this)         │    │
│  │  + Dispatch()     → delegates to state.Dispatch(this)        │    │
│  │  + Cancel()       → delegates to state.Cancel(this)          │    │
│  │  + TransitionTo(newState)  → changes current state           │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                │                                     │
│                                │ uses                                │
│                                ▼                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │              <<interface>> IOrderState                       │    │
│  │  ──────────────────────────────────────────────────────────  │    │
│  │  + Confirm(context: Order)                                   │    │
│  │  + Dispatch(context: Order)                                  │    │
│  │  + Cancel(context: Order)                                    │    │
│  │  + UpdateStatus(context: Order, status: string)              │    │
│  │  + CanTransitionTo(targetState: OrderStatus): bool           │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                │                                     │
│         ┌──────────────────────┼──────────────────────┐             │
│         │           │          │          │           │             │
│         ▼           ▼          ▼          ▼           ▼             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │
│  │ Created  │ │Confirmed │ │Dispatched│ │InTransit │ │ Delivered│  │
│  │  State   │ │  State   │ │  State   │ │  State   │ │  State   │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘  │
│                                                                      │
│  Each state implements what actions are valid in that state         │
│  Invalid actions throw InvalidStateTransitionException              │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Why State Pattern?

| Benefit | Order Processing Application |
|---------|------------------------------|
| **Encapsulates state-specific behavior** | Each state knows what's valid |
| **Eliminates complex conditionals** | No giant switch/if-else |
| **Makes transitions explicit** | Clear state machine |
| **Easy to add new states** | Just add new state class |
| **Self-documenting** | State classes are documentation |

---

## 🔄 Enhanced State Machine - Reverse Logistics

### Why Enhancement Needed?

Modern e-commerce requires handling complex scenarios:
- **Partial Shipments**: Large orders split across warehouses (京东多仓发货)
- **Reverse Logistics**: Returns, refunds, exchanges (逆向物流)
- **Split Delivery**: Some items shipped, others backordered

### Extended Order Status Enum

| Value | Name | Description | Cancellable | Modifiable | Flow |
|-------|------|-------------|-------------|------------|------|
| 0 | CREATED | Order created | ✅ | ✅ | Forward |
| 1 | CONFIRMED | Payment received | ✅ | ✅ | Forward |
| 2 | DISPATCHED | Carrier booked | ✅ | ⬜ | Forward |
| 3 | IN_TRANSIT | Carrier picked up | ⬜ | ⬜ | Forward |
| 4 | OUT_FOR_DELIVERY | Last mile delivery | ⬜ | ⬜ | Forward |
| 5 | DELIVERED | Successfully delivered | ⬜ | ⬜ | Forward |
| 6 | FAILED_DELIVERY | Delivery attempt failed | ⬜ | ⬜ | Forward |
| 7 | CANCELLED | Order cancelled | ⬜ | ⬜ | Terminal |
| 8 | EXCEPTION | Problem detected | ⬜ | ⬜ | Forward |
| **9** | **PARTIALLY_SHIPPED** | **Some items shipped** | ⬜ | ⬜ | **Forward** |
| **10** | **RETURN_REQUESTED** | **Customer requests return** | ⬜ | ⬜ | **Reverse** |
| **11** | **RETURN_IN_TRANSIT** | **Return shipment moving** | ⬜ | ⬜ | **Reverse** |
| **12** | **RETURN_RECEIVED** | **Warehouse received return** | ⬜ | ⬜ | **Reverse** |
| **13** | **REFUNDED** | **Refund processed** | ⬜ | ⬜ | **Terminal** |

### Enhanced State Diagram with Reverse Flow

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                    ENHANCED ORDER STATE MACHINE (正向 + 逆向物流)                  │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ╔═══════════════════════════════════════════════════════════════════════════╗  │
│  ║                          FORWARD FLOW (正向物流)                           ║  │
│  ╚═══════════════════════════════════════════════════════════════════════════╝  │
│                                                                                  │
│                              ┌─────────┐                                        │
│                              │ CREATED │                                        │
│                              └────┬────┘                                        │
│                    payment        │      cancel                                  │
│                    received       │      requested                               │
│                     ┌─────────────┴─────────────┐                               │
│                     ▼                           ▼                                │
│               ┌───────────┐               ┌───────────┐                         │
│               │ CONFIRMED │               │ CANCELLED │                         │
│               └─────┬─────┘               └───────────┘                         │
│                     │                           ▲                                │
│          ┌─────────┴─────────┐                 │ cancel                         │
│          │                   │                 │ (if allowed)                    │
│          ▼                   ▼                 │                                 │
│  ┌─────────────────┐   ┌────────────┐         │                                 │
│  │ PARTIALLY_      │   │ DISPATCHED │─────────┤                                 │
│  │ SHIPPED         │   └─────┬──────┘         │                                 │
│  │ (多仓发货)       │         │                 │                                 │
│  └────────┬────────┘         │ carrier        │                                 │
│           │ all items        │ pickup         │                                 │
│           │ shipped          │                │                                 │
│           └──────────────────┼────────────────┤                                 │
│                              ▼                │                                 │
│                        ┌────────────┐         │                                 │
│                        │ IN_TRANSIT │─────────┤                                 │
│                        └─────┬──────┘         │                                 │
│                              │ out for        │                                 │
│                              │ delivery       │                                 │
│                              ▼                                                   │
│                   ┌──────────────────────┐                                      │
│                   │  OUT_FOR_DELIVERY    │                                      │
│                   └──────────┬───────────┘                                      │
│                     success  │     failure                                       │
│                     ┌────────┴────────┐                                         │
│                     ▼                 ▼                                          │
│               ┌───────────┐    ┌────────────────┐                               │
│               │ DELIVERED │    │ FAILED_DELIVERY│◄──┐                           │
│               │  (Final)  │    └───────┬────────┘   │ retry                     │
│               └─────┬─────┘            │            │                           │
│                     │                  └────────────┘                           │
│                     │                                                            │
│  ╔══════════════════╧════════════════════════════════════════════════════════╗  │
│  ║                          REVERSE FLOW (逆向物流)                           ║  │
│  ╚═══════════════════════════════════════════════════════════════════════════╝  │
│                     │                                                            │
│                     │ return                                                     │
│                     │ requested                                                  │
│                     ▼                                                            │
│            ┌─────────────────┐                                                  │
│            │ RETURN_REQUESTED│  ◄─── Customer initiates return (7天无理由)       │
│            │ (退货申请)       │                                                   │
│            └────────┬────────┘                                                  │
│                     │ approved &                                                 │
│                     │ pickup scheduled                                           │
│                     ▼                                                            │
│            ┌─────────────────┐                                                  │
│            │ RETURN_IN_      │  ◄─── Reverse logistics carrier                  │
│            │ TRANSIT         │       (逆向物流承运中)                             │
│            │ (退货运输中)     │                                                   │
│            └────────┬────────┘                                                  │
│                     │ warehouse                                                  │
│                     │ received                                                   │
│                     ▼                                                            │
│            ┌─────────────────┐                                                  │
│            │ RETURN_RECEIVED │  ◄─── QC inspection passed                       │
│            │ (退货已签收)     │       (质检通过)                                   │
│            └────────┬────────┘                                                  │
│                     │ refund                                                     │
│                     │ processed                                                  │
│                     ▼                                                            │
│            ┌─────────────────┐                                                  │
│            │    REFUNDED     │  ◄─── Final state for returns                    │
│            │   (已退款)       │       (退款完成)                                   │
│            └─────────────────┘                                                  │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Reverse Logistics State Implementations

```
┌─────────────────────────────────────────────────────────────────────────────┐
│              REVERSE LOGISTICS STATE CLASSES (逆向物流状态类)                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  ReturnRequestedState : IOrderState                                   │  │
│  │  ───────────────────────────────────────────────────────────────────  │  │
│  │  Properties:                                                          │  │
│  │    - Status = OrderStatus.RETURN_REQUESTED                            │  │
│  │    - CanCancel = false    // Cannot cancel return in progress         │  │
│  │    - CanModify = false                                                │  │
│  │                                                                       │  │
│  │  Allowed Transitions:                                                 │  │
│  │    → RETURN_IN_TRANSIT (ApproveReturn + SchedulePickup)               │  │
│  │    → CANCELLED (RejectReturn - item not eligible)                     │  │
│  │                                                                       │  │
│  │  Key Methods:                                                         │  │
│  │    + ApproveReturn(context, pickupDate)                               │  │
│  │      → Validate return eligibility (7-day policy)                     │  │
│  │      → Book reverse logistics carrier                                 │  │
│  │      → TransitionTo(ReturnInTransitState)                             │  │
│  │                                                                       │  │
│  │    + RejectReturn(context, reason)                                    │  │
│  │      → Record rejection reason                                        │  │
│  │      → Notify customer                                                │  │
│  │      → TransitionTo(DeliveredState) // Back to delivered              │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  ReturnInTransitState : IOrderState                                   │  │
│  │  ───────────────────────────────────────────────────────────────────  │  │
│  │  Properties:                                                          │  │
│  │    - Status = OrderStatus.RETURN_IN_TRANSIT                           │  │
│  │    - ReturnTrackingNumber: string  // Reverse shipment tracking       │  │
│  │    - ReturnCarrierCode: string                                        │  │
│  │                                                                       │  │
│  │  Allowed Transitions:                                                 │  │
│  │    → RETURN_RECEIVED (WarehouseConfirmsReceipt)                       │  │
│  │    → EXCEPTION (LostInTransit, DamagedInTransit)                      │  │
│  │                                                                       │  │
│  │  Key Methods:                                                         │  │
│  │    + ConfirmReceipt(context, qcResult)                                │  │
│  │      → Validate QC inspection                                         │  │
│  │      → Update inventory                                               │  │
│  │      → TransitionTo(ReturnReceivedState)                              │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  ReturnReceivedState : IOrderState                                    │  │
│  │  ───────────────────────────────────────────────────────────────────  │  │
│  │  Properties:                                                          │  │
│  │    - Status = OrderStatus.RETURN_RECEIVED                             │  │
│  │    - QCResult: QualityCheckResult                                     │  │
│  │    - RefundEligible: bool                                             │  │
│  │                                                                       │  │
│  │  Allowed Transitions:                                                 │  │
│  │    → REFUNDED (ProcessRefund)                                         │  │
│  │    → EXCEPTION (QCFailed - partial refund)                            │  │
│  │                                                                       │  │
│  │  Key Methods:                                                         │  │
│  │    + ProcessRefund(context, amount)                                   │  │
│  │      → Calculate refund (full or partial based on QC)                 │  │
│  │      → Call payment service                                           │  │
│  │      → Publish OrderRefunded event                                    │  │
│  │      → TransitionTo(RefundedState)                                    │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  PartiallyShippedState : IOrderState                                  │  │
│  │  ───────────────────────────────────────────────────────────────────  │  │
│  │  Scenario: 京东/天猫大促期间，同一订单从多个仓库发货                        │  │
│  │                                                                       │  │
│  │  Properties:                                                          │  │
│  │    - Status = OrderStatus.PARTIALLY_SHIPPED                           │  │
│  │    - ShippedItems: List<ShippedItem>     // Items already shipped     │  │
│  │    - PendingItems: List<PendingItem>     // Items awaiting shipment   │  │
│  │    - Shipments: List<Shipment>           // Multiple tracking numbers │  │
│  │                                                                       │  │
│  │  Allowed Transitions:                                                 │  │
│  │    → PARTIALLY_SHIPPED (ShipMoreItems - still items pending)          │  │
│  │    → IN_TRANSIT (AllItemsShipped - all items now in transit)          │  │
│  │    → EXCEPTION (BackorderTimeout - items unavailable)                 │  │
│  │                                                                       │  │
│  │  Key Methods:                                                         │  │
│  │    + ShipItems(context, items, shipment)                              │  │
│  │      → Add items to ShippedItems                                      │  │
│  │      → Remove from PendingItems                                       │  │
│  │      → Add new Shipment with tracking                                 │  │
│  │      → If (PendingItems.Count == 0) TransitionTo(InTransitState)      │  │
│  │      → Else: stay in PARTIALLY_SHIPPED                                │  │
│  │                                                                       │  │
│  │  Example Flow (双11多仓发货):                                          │  │
│  │    Order: iPhone + AirPods + Case                                     │  │
│  │    → Day 1: iPhone shipped from Shanghai (SF Express)                 │  │
│  │    → Day 2: AirPods shipped from Shenzhen (JD Logistics)              │  │
│  │    → Day 3: Case shipped from Guangzhou (ZTO)                         │  │
│  │    → All items delivered → Order DELIVERED                            │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Reverse Logistics Domain Events

| Event | Trigger | Payload | Subscribers |
|-------|---------|---------|-------------|
| ReturnRequested | Customer initiates | OrderId, Reason, Items | CS, Warehouse |
| ReturnApproved | CS approves | OrderId, PickupDate | Logistics, Customer |
| ReturnRejected | CS rejects | OrderId, RejectionReason | Customer |
| ReturnPickedUp | Carrier pickup | OrderId, ReturnTrackingNo | Tracking |
| ReturnReceived | Warehouse scan | OrderId, QCResult | Finance, Inventory |
| RefundProcessed | Payment complete | OrderId, Amount, Method | Customer, Analytics |

---

## 🔄 CQRS Architecture

### CQRS Structure

```
┌─────────────────────────────────────────────────────────────────────┐
│                       CQRS ARCHITECTURE                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│                         ┌───────────┐                               │
│                         │  Client   │                               │
│                         └─────┬─────┘                               │
│                               │                                      │
│              ┌────────────────┴────────────────┐                    │
│              │                                 │                    │
│              ▼                                 ▼                    │
│  ┌───────────────────────┐       ┌───────────────────────┐         │
│  │      COMMANDS         │       │       QUERIES         │         │
│  │    (Write Side)       │       │     (Read Side)       │         │
│  │                       │       │                       │         │
│  │  CreateOrderCommand   │       │  GetOrderByIdQuery    │         │
│  │  DispatchOrderCommand │       │  ListOrdersQuery      │         │
│  │  CancelOrderCommand   │       │  GetOrderStatusQuery  │         │
│  │  UpdateAddressCommand │       │  SearchOrdersQuery    │         │
│  └───────────┬───────────┘       └───────────┬───────────┘         │
│              │                               │                      │
│              ▼                               ▼                      │
│  ┌───────────────────────┐       ┌───────────────────────┐         │
│  │   Command Handlers    │       │    Query Handlers     │         │
│  │                       │       │                       │         │
│  │  - Validate           │       │  - No validation      │         │
│  │  - Execute business   │       │  - Direct DB read     │         │
│  │    logic              │       │  - Optimized queries  │         │
│  │  - Publish events     │       │  - Cached results     │         │
│  └───────────┬───────────┘       └───────────┬───────────┘         │
│              │                               │                      │
│              ▼                               ▼                      │
│  ┌───────────────────────┐       ┌───────────────────────┐         │
│  │    Write Database     │       │    Read Database      │         │
│  │                       │       │    (or same DB with   │         │
│  │  - Normalized         │       │     optimized views)  │         │
│  │  - Transactional      │       │                       │         │
│  │  - Consistent         │       │  - Denormalized       │         │
│  │                       │       │  - Fast queries       │         │
│  └───────────────────────┘       └───────────────────────┘         │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Why CQRS?

| Benefit | Order Processing Application |
|---------|------------------------------|
| **Separate scaling** | Scale reads independently from writes |
| **Optimized queries** | Read models tuned for specific views |
| **Simpler commands** | Commands focus on business logic |
| **Better performance** | Reads don't lock write tables |
| **Event sourcing ready** | Natural fit for event-driven |

---

## 🔄 Deep CQRS - Read/Write Separation

### Complete CQRS Architecture with Sync Strategy

```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│                    DEEP CQRS ARCHITECTURE (深度读写分离)                              │
├──────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│                              ┌─────────────────┐                                     │
│                              │   API Gateway   │                                     │
│                              │   (Kong/Nginx)  │                                     │
│                              └────────┬────────┘                                     │
│                                       │                                              │
│                    ┌──────────────────┴──────────────────┐                           │
│                    │                                     │                           │
│                    ▼                                     ▼                           │
│  ╔═══════════════════════════════════╗   ╔═══════════════════════════════════╗       │
│  ║      WRITE SIDE (写端)            ║   ║       READ SIDE (读端)            ║      │
│  ║      /api/v1/orders/commands      ║   ║       /api/v1/orders/queries      ║     │
│  ╠═══════════════════════════════════╣   ╠═══════════════════════════════════╣     │
│  ║                                   ║   ║                                   ║     │
│  ║  ┌─────────────────────────────┐  ║   ║  ┌─────────────────────────────┐  ║     │
│  ║  │     Command Controller      │  ║   ║  │      Query Controller       │  ║     │
│  ║  │  - POST /orders             │  ║   ║  │  - GET /orders/{id}         │  ║     │
│  ║  │  - PUT /orders/{id}/confirm │  ║   ║  │  - GET /orders              │  ║     │
│  ║  │  - DELETE /orders/{id}      │  ║   ║  │  - GET /orders/search       │  ║     │
│  ║  └──────────────┬──────────────┘  ║   ║  └──────────────┬──────────────┘  ║     │
│  ║                 │                 ║   ║                 │                 ║     │
│  ║                 ▼                 ║   ║                 ▼                 ║     │
│  ║  ┌─────────────────────────────┐  ║   ║  ┌─────────────────────────────┐  ║     │
│  ║  │      MediatR Pipeline       │  ║   ║  │      MediatR Pipeline       │  ║     │
│  ║  │  ┌─────────────────────┐    │  ║   ║  │  ┌─────────────────────┐    │  ║     │
│  ║  │  │ ValidationBehavior  │    │  ║   ║  │  │   CachingBehavior   │    │  ║     │
│  ║  │  │ LoggingBehavior     │    │  ║   ║  │  │   LoggingBehavior   │    │  ║     │
│  ║  │  │ TransactionBehavior │    │  ║   ║  │  └─────────────────────┘    │  ║     │
│  ║  │  └─────────────────────┘    │  ║   ║  └──────────────┬──────────────┘  ║     │
│  ║  └──────────────┬──────────────┘  ║   ║                 │                 ║     │
│  ║                 │                 ║   ║                 ▼                 ║     │
│  ║                 ▼                 ║   ║  ┌─────────────────────────────┐  ║     │
│  ║  ┌─────────────────────────────┐  ║   ║  │      Query Handlers         │  ║     │
│  ║  │     Command Handlers        │  ║   ║  │                             │  ║     │
│  ║  │  - Business validation      │  ║   ║  │  - Direct DB read           │  ║     │
│  ║  │  - Domain logic             │  ║   ║  │  - Optimized projections    │  ║     │
│  ║  │  - State transitions        │  ║   ║  │  - No domain logic          │  ║     │
│  ║  │  - Event publishing         │  ║   ║  │  - Redis cache integration  │  ║     │
│  ║  └──────────────┬──────────────┘  ║   ║  └──────────────┬──────────────┘  ║     │
│  ║                 │                 ║   ║                 │                 ║     │
│  ║                 ▼                 ║   ║                 ▼                 ║     │
│  ║  ┌─────────────────────────────┐  ║   ║  ┌─────────────────────────────┐  ║     │
│  ║  │   WRITE MODEL (写模型)      │  ║   ║  │    READ MODEL (读模型)       │  ║     │
│  ║  │                             │  ║   ║  │                             │  ║     │
│  ║  │  ┌─────────────────────┐    │  ║   ║  │  ┌─────────────────────┐    │  ║     │
│  ║  │  │   Order Aggregate   │    │  ║   ║  │  │   OrderListView     │    │  ║     │
│  ║  │  │   - Rich domain     │    │  ║   ║  │  │   (Flat, indexed)   │    │  ║     │
│  ║  │  │   - State machine   │    │  ║   ║  │  ├─────────────────────┤    │  ║     │
│  ║  │  │   - Business rules  │    │  ║   ║  │  │   OrderDetailView   │    │  ║     │
│  ║  │  │   - Invariants      │    │  ║   ║  │  │   (Denormalized)    │    │  ║     │
│  ║  │  └─────────────────────┘    │  ║   ║  │  ├─────────────────────┤    │  ║     │
│  ║  │                             │  ║   ║  │  │   OrderStatsView    │    │  ║     │
│  ║  │  Tables:                    │  ║   ║  │  │   (Pre-aggregated)  │    │  ║     │
│  ║  │  - Orders (normalized)      │  ║   ║  │  └─────────────────────┘    │  ║     │
│  ║  │  - OrderItems               │  ║   ║  │                             │  ║     │
│  ║  │  - OrderEvents (audit)      │  ║   ║  │  Sources:                   │  ║     │
│  ║  │                             │  ║   ║  │  - SQL Views                │  ║     │
│  ║  └──────────────┬──────────────┘  ║   ║  │  - Redis Cache              │  ║     │
│  ║                 │                 ║   ║  │  - Elasticsearch            │  ║     │
│  ║                 │ publish         ║   ║  └──────────────▲──────────────┘  ║     │
│  ╚═════════════════╪═════════════════╝   ╚══════════════════╪════════════════╝     │
│                    │                                        │                      │
│                    │              SYNC STRATEGY             │                      │
│                    │         (最终一致性同步策略)             │                      │
│                    │                                        │                      │
│                    ▼                                        │                      │
│  ┌───────────────────────────────────────────────────────────────────────────────┐ │
│  │                        MESSAGE QUEUE (RabbitMQ/Kafka)                         │ │
│  │                                                                               │ │
│  │   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐       │ │
│  │   │ OrderCreated│   │OrderUpdated │   │OrderDeleted │   │OrderStatus  │       │ │
│  │   │   Event     │   │   Event     │   │   Event     │   │  Changed    │       │ │
│  │   └──────┬──────┘   └──────┬──────┘   └──────┬──────┘   └──────┬──────┘       │ │
│  │          │                 │                 │                 │              │ │
│  │          └─────────────────┴─────────────────┴─────────────────┘              │ │
│  │                                    │                                          │ │
│  └────────────────────────────────────┼──────────────────────────────────────────┘ │
│                                       │                                            │
│                                       ▼                                            │
│  ┌───────────────────────────────────────────────────────────────────────────────┐ │
│  │                    READ MODEL UPDATER SERVICE (读模型更新服务)                 │ │
│  │                                                                                │ │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐  │ │
│  │   │  OrderProjectionHandler                                                 │  │ │
│  │   │                                                                         │  │ │
│  │   │  Handle(OrderCreatedEvent):                                             │  │ │
│  │   │    1. Insert into OrderListView                                         │  │ │
│  │   │    2. Update Redis cache (order:{id})                                   │  │ │
│  │   │    3. Index in Elasticsearch (full-text search)                         │  │ │
│  │   │    4. Update OrderStatsView (increment counters)                        │  │ │
│  │   │                                                                         │  │ │
│  │   │  Handle(OrderStatusChangedEvent):                                       │  │ │
│  │   │    1. Update OrderListView.Status                                       │  │ │
│  │   │    2. Invalidate Redis cache                                            │  │ │
│  │   │    3. Update Elasticsearch document                                     │  │ │
│  │   │    4. Update stats (decrement old status, increment new)                │  │ │
│  │   └─────────────────────────────────────────────────────────────────────────┘  │ │
│  │                                                                                │ │
│  │   Consistency Window: ~50-200ms (最终一致性窗口)                                │ │
│  │   Retry Policy: 3 attempts with exponential backoff                           │ │
│  │   Dead Letter Queue: For failed projections                                   │ │
│  │                                                                                │ │
│  └────────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                      │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

### Read Model Synchronization Strategies

| Strategy | Description | Use Case | Consistency | Performance |
|----------|-------------|----------|-------------|-------------|
| **Event-Driven** | Async via message queue | Standard updates | Eventual (~200ms) | ⭐⭐⭐⭐⭐ |
| **Dual-Write** | Write to both synchronously | Critical data | Strong | ⭐⭐ |
| **Change Data Capture** | Database log streaming | Large scale sync | Eventual (~100ms) | ⭐⭐⭐⭐ |
| **Scheduled Rebuild** | Periodic full rebuild | Analytics views | Eventual (minutes) | ⭐⭐⭐ |

### Read Model Projections

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        READ MODEL PROJECTIONS (读模型投影)                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  OrderListView (用于列表页面)                                          │  │
│  │  ───────────────────────────────────────────────────────────────────  │  │
│  │  Purpose: Fast paginated list display                                 │  │
│  │  Storage: SQL Server + Redis                                          │  │
│  │                                                                       │  │
│  │  Schema:                                                              │  │
│  │  ├── Id (GUID, PK, indexed)                                           │  │
│  │  ├── OrderNumber (varchar, unique, indexed)                           │  │
│  │  ├── CustomerName (varchar, indexed)                                  │  │
│  │  ├── Status (int, indexed)                                            │  │
│  │  ├── StatusDisplayName (varchar)  // Denormalized for display         │  │
│  │  ├── TotalAmount (decimal)                                            │  │
│  │  ├── ItemCount (int)              // Denormalized                     │  │
│  │  ├── DestinationCity (varchar, indexed)                               │  │
│  │  ├── TrackingNumber (varchar, indexed)                                │  │
│  │  ├── CarrierName (varchar)        // Denormalized                     │  │
│  │  ├── CreatedAt (datetime, indexed)                                    │  │
│  │  └── LastUpdatedAt (datetime, indexed)                                │  │
│  │                                                                       │  │
│  │  Indexes:                                                             │  │
│  │  - IX_OrderListView_Status_CreatedAt (Status DESC, CreatedAt DESC)    │  │
│  │  - IX_OrderListView_CustomerName (CustomerName)                       │  │
│  │  - IX_OrderListView_TrackingNumber (TrackingNumber)                   │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  OrderDetailView (用于详情页面)                                        │  │
│  │  ───────────────────────────────────────────────────────────────────  │  │
│  │  Purpose: Complete order details without joins                        │  │
│  │  Storage: Redis (JSON) + Elasticsearch                                │  │
│  │                                                                       │  │
│  │  Schema (JSON):                                                       │  │
│  │  {                                                                    │  │
│  │    "id": "guid",                                                      │  │
│  │    "orderNumber": "ORD-20260131-0001",                                │  │
│  │    "status": { "code": 3, "name": "IN_TRANSIT", "display": "运输中" },│  │
│  │    "customer": {                                                      │  │
│  │      "id": "guid",                                                    │  │
│  │      "name": "张三",                                                  │  │
│  │      "phone": "138****8888"  // Masked for security                   │  │
│  │    },                                                                 │  │
│  │    "shippingAddress": {                                               │  │
│  │      "full": "上海市浦东新区...",                                     │  │
│  │      "city": "上海市",                                                │  │
│  │      "coordinates": [121.4737, 31.2304]                               │  │
│  │    },                                                                 │  │
│  │    "items": [                                                         │  │
│  │      { "name": "iPhone 15", "qty": 1, "price": 7999 }                 │  │
│  │    ],                                                                 │  │
│  │    "shipments": [                                                     │  │
│  │      {                                                                │  │
│  │        "trackingNumber": "SF1234567890",                              │  │
│  │        "carrier": "顺丰速运",                                         │  │
│  │        "currentLocation": "上海转运中心",                             │  │
│  │        "estimatedDelivery": "2026-02-01T18:00:00Z"                    │  │
│  │      }                                                                │  │
│  │    ],                                                                 │  │
│  │    "timeline": [                                                      │  │
│  │      { "time": "...", "event": "订单创建", "detail": "..." }          │  │
│  │    ]                                                                  │  │
│  │  }                                                                    │  │
│  │                                                                       │  │
│  │  TTL: 30 minutes (auto-refresh on access)                             │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  OrderStatsView (用于仪表盘)                                           │  │
│  │  ───────────────────────────────────────────────────────────────────  │  │
│  │  Purpose: Pre-aggregated statistics for dashboards                    │  │
│  │  Storage: SQL Server (Materialized View) + Redis                      │  │
│  │                                                                       │  │
│  │  Daily Stats:                                                         │  │
│  │  ├── Date (date, PK)                                                  │  │
│  │  ├── TotalOrders (int)                                                │  │
│  │  ├── TotalRevenue (decimal)                                           │  │
│  │  ├── OrdersByStatus (JSON) // {"CREATED": 10, "DELIVERED": 50, ...}   │  │
│  │  ├── OrdersByCarrier (JSON)                                           │  │
│  │  ├── AvgDeliveryTime (decimal) // hours                               │  │
│  │  └── LastUpdatedAt (datetime)                                         │  │
│  │                                                                       │  │
│  │  Refresh Strategy: Event-driven + Hourly reconciliation               │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Write Model vs Read Model Comparison

| Aspect | Write Model (写模型) | Read Model (读模型) |
|--------|----------------------|---------------------|
| **Purpose** | Enforce business rules | Optimize queries |
| **Structure** | Normalized, aggregate-oriented | Denormalized, query-oriented |
| **Validation** | Full domain validation | None (pre-validated) |
| **Consistency** | Strong (transactional) | Eventual (~50-200ms) |
| **Scaling** | Vertical (complex logic) | Horizontal (stateless reads) |
| **Technology** | SQL Server + EF Core | Redis + Elasticsearch + Views |
| **Example** | `Order` aggregate with `OrderItems` | `OrderDetailView` (flat JSON) |

---

## 🧠 Smart Dispatch Algorithm

### Overview: 3-Layer Decision System (三层决策系统)

智能分单是现代物流系统的核心能力，参考京东物流智慧大脑、菜鸟网络分单引擎设计。

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                    SMART DISPATCH ALGORITHM (智能分单算法)                         │
├──────────────────────────────────────────────────────────────────────────────────┤
│                                                                                   │
│                           ┌─────────────────────┐                                │
│                           │   New Order Input   │                                │
│                           │   (订单输入)         │                                │
│                           └──────────┬──────────┘                                │
│                                      │                                           │
│  ╔═══════════════════════════════════╧═══════════════════════════════════════╗   │
│  ║                    LAYER 1: RULE FILTERING (规则过滤层)                    ║   │
│  ║                        Pattern: Specification + Chain of Responsibility   ║   │
│  ╠═══════════════════════════════════════════════════════════════════════════╣   │
│  ║                                                                           ║   │
│  ║  ┌─────────────────────────────────────────────────────────────────────┐  ║   │
│  ║  │  Business Rules (业务规则) - Hard Constraints                       │  ║   │
│  ║  │                                                                     │  ║   │
│  ║  │  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐             │  ║   │
│  ║  │  │ Geographic  │───▶│ Service     │───▶│ Carrier     │             │  ║   │
│  ║  │  │ Coverage    │    │ Capability  │    │ Contract    │             │  ║   │
│  ║  │  │ (覆盖区域)   │    │ (服务能力)   │    │ (合同限制)   │             │  ║   │
│  ║  │  └─────────────┘    └─────────────┘    └─────────────┘             │  ║   │
│  ║  │                                                                     │  ║   │
│  ║  │  Examples:                                                          │  ║   │
│  ║  │  - 顺丰冷链：仅限冷链服务区域                                         │  ║   │
│  ║  │  - 京东物流：优先自营仓覆盖区域                                       │  ║   │
│  ║  │  - 中通：偏远地区费用加成                                            │  ║   │
│  ║  │  - 国际件：需海关清关资质承运商                                       │  ║   │
│  ║  └─────────────────────────────────────────────────────────────────────┘  ║   │
│  ║                                                                           ║   │
│  ║  Input: All carriers (10+)                                                ║   │
│  ║  Output: Eligible carriers (3-5)                                          ║   │
│  ║                                                                           ║   │
│  ╚═══════════════════════════════════════════════════════════════════════════╝   │
│                                      │                                           │
│                                      ▼                                           │
│  ╔═══════════════════════════════════════════════════════════════════════════╗   │
│  ║                    LAYER 2: STRATEGY SCORING (策略评分层)                  ║   │
│  ║                        Pattern: Strategy + Template Method                ║   │
│  ╠═══════════════════════════════════════════════════════════════════════════╣   │
│  ║                                                                           ║   │
│  ║  ┌─────────────────────────────────────────────────────────────────────┐  ║   │
│  ║  │  Scoring Strategies (评分策略) - Soft Constraints with Weights      │  ║   │
│  ║  │                                                                     │  ║   │
│  ║  │  ┌───────────────────────────────────────────────────────────────┐  │  ║   │
│  ║  │  │  IDispatchScoringStrategy                                     │  │  ║   │
│  ║  │  │  + Score(order, carrier) : decimal                            │  │  ║   │
│  ║  │  │  + Weight : decimal                                           │  │  ║   │
│  ║  │  └───────────────────────────────────────────────────────────────┘  │  ║   │
│  ║  │           │                                                         │  ║   │
│  ║  │           ├── CostScoringStrategy (成本评分)                        │  ║   │
│  ║  │           │     Weight: 0.30                                        │  ║   │
│  ║  │           │     Factors: Base rate, surcharges, fuel cost           │  ║   │
│  ║  │           │                                                         │  ║   │
│  ║  │           ├── SpeedScoringStrategy (时效评分)                       │  ║   │
│  ║  │           │     Weight: 0.25                                        │  ║   │
│  ║  │           │     Factors: Transit time, delivery window              │  ║   │
│  ║  │           │                                                         │  ║   │
│  ║  │           ├── ReliabilityScoringStrategy (可靠性评分)               │  ║   │
│  ║  │           │     Weight: 0.20                                        │  ║   │
│  ║  │           │     Factors: On-time rate, damage rate, loss rate       │  ║   │
│  ║  │           │                                                         │  ║   │
│  ║  │           ├── CapacityScoringStrategy (运力评分)                    │  ║   │
│  ║  │           │     Weight: 0.15                                        │  ║   │
│  ║  │           │     Factors: Available capacity, peak season load       │  ║   │
│  ║  │           │                                                         │  ║   │
│  ║  │           └── CustomerPreferenceScoringStrategy (客户偏好评分)       │  ║   │
│  ║  │                 Weight: 0.10                                        │  ║   │
│  ║  │                 Factors: Historical preference, explicit selection  │  ║   │
│  ║  │                                                                     │  ║   │
│  ║  │  Total Score = Σ (Strategy.Score × Strategy.Weight)                 │  ║   │
│  ║  │                                                                     │  ║   │
│  ║  └─────────────────────────────────────────────────────────────────────┘  ║   │
│  ║                                                                           ║   │
│  ║  Input: Eligible carriers (3-5)                                           ║   │
│  ║  Output: Ranked carriers with scores                                      ║   │
│  ║                                                                           ║   │
│  ╚═══════════════════════════════════════════════════════════════════════════╝   │
│                                      │                                           │
│                                      ▼                                           │
│  ╔═══════════════════════════════════════════════════════════════════════════╗   │
│  ║                    LAYER 3: DECISION EXECUTION (决策执行层)                ║   │
│  ║                        Pattern: Factory + State                           ║   │
│  ╠═══════════════════════════════════════════════════════════════════════════╣   │
│  ║                                                                           ║   │
│  ║  ┌─────────────────────────────────────────────────────────────────────┐  ║   │
│  ║  │  Decision Modes (决策模式)                                          │  ║   │
│  ║  │                                                                     │  ║   │
│  ║  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐     │  ║   │
│  ║  │  │ AUTO_DISPATCH   │  │ MANUAL_REVIEW   │  │ SPLIT_DISPATCH  │     │  ║   │
│  ║  │  │ (自动分单)       │  │ (人工审核)       │  │ (拆单分配)       │     │  ║   │
│  ║  │  │                 │  │                 │  │                 │     │  ║   │
│  ║  │  │ Score > 0.8:    │  │ Score 0.5-0.8:  │  │ Large orders:   │     │  ║   │
│  ║  │  │ Direct assign   │  │ Queue for human │  │ Multi-carrier   │     │  ║   │
│  ║  │  │ to top carrier  │  │ dispatcher      │  │ assignment      │     │  ║   │
│  ║  │  └─────────────────┘  └─────────────────┘  └─────────────────┘     │  ║   │
│  ║  │                                                                     │  ║   │
│  ║  │  Special Handling:                                                  │  ║   │
│  ║  │  - 双11/618: Lower auto-dispatch threshold to 0.7                   │  ║   │
│  ║  │  - VIP客户: Always manual review for orders > ¥10,000               │  ║   │
│  ║  │  - 新承运商: First 100 orders require manual approval               │  ║   │
│  ║  │                                                                     │  ║   │
│  ║  └─────────────────────────────────────────────────────────────────────┘  ║   │
│  ║                                                                           ║   │
│  ║  Output: Dispatch decision + Assigned carrier(s)                          ║   │
│  ║                                                                           ║   │
│  ╚═══════════════════════════════════════════════════════════════════════════╝   │
│                                      │                                           │
│                                      ▼                                           │
│                           ┌─────────────────────┐                                │
│                           │  Dispatch Command   │                                │
│                           │  → State Transition │                                │
│                           │  → Event Published  │                                │
│                           └─────────────────────┘                                │
└──────────────────────────────────────────────────────────────────────────────────┘
```

### Rule Engine Integration (规则引擎集成)

Using Microsoft RulesEngine for flexible business rule management:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    RULES ENGINE CONFIGURATION (规则引擎配置)                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  Configuration File: dispatch-rules.json                                     │
│  {                                                                           │
│    "WorkflowName": "DispatchEligibilityWorkflow",                            │
│    "Rules": [                                                                │
│      {                                                                       │
│        "RuleName": "ColdChainRequirement",                                   │
│        "Expression": "order.RequiresColdChain == true",                      │
│        "SuccessEvent": "FilterToColdChainCarriers",                          │
│        "Actions": {                                                          │
│          "OnSuccess": {                                                      │
│            "Name": "FilterCarriers",                                         │
│            "Context": { "CarrierCapability": "COLD_CHAIN" }                  │
│          }                                                                   │
│        }                                                                     │
│      },                                                                      │
│      {                                                                       │
│        "RuleName": "RemoteAreaSurcharge",                                    │
│        "Expression": "destination.IsRemoteArea == true",                     │
│        "SuccessEvent": "ApplyRemoteSurcharge",                               │
│        "Actions": {                                                          │
│          "OnSuccess": {                                                      │
│            "Name": "AdjustCost",                                             │
│            "Context": { "SurchargePercent": 30 }                             │
│          }                                                                   │
│        }                                                                     │
│      },                                                                      │
│      {                                                                       │
│        "RuleName": "Double11Capacity",                                       │
│        "Expression": "DateTime.Now.Month == 11 && DateTime.Now.Day <= 15",   │
│        "SuccessEvent": "ApplyPeakSeasonRules",                               │
│        "Actions": {                                                          │
│          "OnSuccess": {                                                      │
│            "Name": "AdjustCapacityWeight",                                   │
│            "Context": { "CapacityWeight": 0.30 }  // Increase from 0.15      │
│          }                                                                   │
│        }                                                                     │
│      },                                                                      │
│      {                                                                       │
│        "RuleName": "HazmatRestriction",                                      │
│        "Expression": "order.ContainsHazmat == true",                         │
│        "SuccessEvent": "FilterToHazmatCarriers",                             │
│        "Actions": {                                                          │
│          "OnSuccess": {                                                      │
│            "Name": "FilterCarriers",                                         │
│            "Context": { "CarrierCapability": "HAZMAT_CERTIFIED" }            │
│          }                                                                   │
│        }                                                                     │
│      }                                                                       │
│    ]                                                                         │
│  }                                                                           │
│                                                                              │
│  Benefits:                                                                   │
│  - Rules configurable without code deployment                                │
│  - Business users can modify rules via admin UI                              │
│  - Version control and audit trail for rule changes                          │
│  - A/B testing different rule configurations                                 │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Scoring Algorithm Implementation

```
Scoring Formula (评分公式):

Final_Score = Σ (Si × Wi × Ai)

Where:
  Si = Individual strategy score (0.0 - 1.0)
  Wi = Strategy weight (configured, Σ Wi = 1.0)
  Ai = Adjustment factor (context-dependent)

Example Calculation (示例计算):
┌─────────────────────────────────────────────────────────────────────────────┐
│  Order: Shanghai → Beijing, Express, 2kg, Electronics                       │
│                                                                             │
│  Carrier: SF Express (顺丰速运)                                              │
│  ├── Cost Score:        0.75 × 0.30 × 1.0 = 0.225                           │
│  ├── Speed Score:       0.95 × 0.25 × 1.0 = 0.2375                          │
│  ├── Reliability Score: 0.98 × 0.20 × 1.0 = 0.196                           │
│  ├── Capacity Score:    0.80 × 0.15 × 1.0 = 0.12                            │
│  └── Preference Score:  0.90 × 0.10 × 1.0 = 0.09                            │
│  ──────────────────────────────────────────                                 │
│  Total Score: 0.8685 → AUTO_DISPATCH ✅                                     │
│                                                                             │
│  Carrier: ZTO (中通快递)                                                     │
│  ├── Cost Score:        0.95 × 0.30 × 1.0 = 0.285                           │
│  ├── Speed Score:       0.70 × 0.25 × 1.0 = 0.175                           │
│  ├── Reliability Score: 0.85 × 0.20 × 1.0 = 0.17                            │
│  ├── Capacity Score:    0.90 × 0.15 × 1.0 = 0.135                           │
│  └── Preference Score:  0.60 × 0.10 × 1.0 = 0.06                            │
│  ──────────────────────────────────────────                                 │
│  Total Score: 0.825 → AUTO_DISPATCH                                         │
│                                                                             │
│  Decision: SF Express selected (higher total score)                         │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Interface Definitions

```
Dispatch Interfaces:
┌────────────────────────────────────────────────────────────────────────────┐
│ IDispatchRuleEngine                                                        │
│   - Namespace: DT.Express.Domain.Orders.Dispatch                           │
│   - Purpose: Filter carriers based on hard business rules                  │
│                                                                            │
│   Methods:                                                                 │
│   + FilterEligibleCarriers(order: Order, carriers: IEnumerable<Carrier>)   │
│     → Returns: IEnumerable<Carrier> (filtered list)                        │
│                                                                            │
│   + EvaluateRules(order: Order)                                            │
│     → Returns: RuleEvaluationResult (which rules matched)                  │
├────────────────────────────────────────────────────────────────────────────┤
│ IDispatchScoringStrategy                                                   │
│   - Namespace: DT.Express.Domain.Orders.Dispatch                           │
│   - Purpose: Score a carrier for a specific order                          │
│                                                                            │
│   Properties:                                                              │
│   + Name: string (strategy identifier)                                     │
│   + Weight: decimal (0.0 - 1.0)                                            │
│                                                                            │
│   Methods:                                                                 │
│   + Score(order: Order, carrier: Carrier)                                  │
│     → Returns: decimal (0.0 - 1.0)                                         │
├────────────────────────────────────────────────────────────────────────────┤
│ IDispatchDecisionMaker                                                     │
│   - Namespace: DT.Express.Domain.Orders.Dispatch                           │
│   - Purpose: Make final dispatch decision                                  │
│                                                                            │
│   Methods:                                                                 │
│   + MakeDecision(order: Order, rankedCarriers: IEnumerable<ScoredCarrier>) │
│     → Returns: DispatchDecision                                            │
│       - Mode: AutoDispatch | ManualReview | SplitDispatch                  │
│       - AssignedCarriers: List<Carrier>                                    │
│       - Reason: string                                                     │
├────────────────────────────────────────────────────────────────────────────┤
│ ISmartDispatchService (Facade)                                             │
│   - Namespace: DT.Express.Application.Orders                               │
│   - Purpose: Orchestrate the 3-layer dispatch process                      │
│                                                                            │
│   Methods:                                                                 │
│   + DispatchOrder(order: Order)                                            │
│     → Returns: DispatchResult                                              │
│       1. Call RuleEngine.FilterEligibleCarriers()                          │
│       2. Call each ScoringStrategy.Score()                                 │
│       3. Call DecisionMaker.MakeDecision()                                 │
│       4. Execute dispatch command                                          │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 📜 Interface Contracts

### IOrderState (State Interface)

```
Interface: IOrderState
Namespace: DT.Express.Domain.Orders.States
Purpose: Define contract for order state behavior

Methods:
┌────────────────────────────────────────────────────────────┐
│ void Confirm(Order context)                                │
│   - Transition to CONFIRMED state                          │
│   - Throws: InvalidStateTransitionException if not allowed │
├────────────────────────────────────────────────────────────┤
│ void Dispatch(Order context)                               │
│   - Transition to DISPATCHED state                         │
│   - Triggers carrier booking                               │
├────────────────────────────────────────────────────────────┤
│ void Cancel(Order context, string reason)                  │
│   - Transition to CANCELLED state                          │
│   - May trigger carrier cancellation                       │
├────────────────────────────────────────────────────────────┤
│ void UpdateFromTracking(Order context, TrackingUpdate upd) │
│   - Handle tracking status changes                         │
├────────────────────────────────────────────────────────────┤
│ bool CanCancel { get; }                                    │
│   - Returns whether cancellation allowed in this state     │
├────────────────────────────────────────────────────────────┤
│ bool CanModify { get; }                                    │
│   - Returns whether modifications allowed                  │
├────────────────────────────────────────────────────────────┤
│ OrderStatus Status { get; }                                │
│   - Returns the enum value for this state                  │
└────────────────────────────────────────────────────────────┘
```

### Command/Query Interfaces (MediatR)

```
Commands (implement IRequest<TResponse>):
┌────────────────────────────────────────────────────────────┐
│ CreateOrderCommand : IRequest<OrderResult>                 │
│   - CustomerInfo, ShippingAddress, Items, ServiceLevel     │
│   - Returns: OrderId, OrderNumber, Status                  │
├────────────────────────────────────────────────────────────┤
│ DispatchOrderCommand : IRequest<DispatchResult>            │
│   - OrderId, PreferredCarrier (optional)                   │
│   - Returns: TrackingNumber, CarrierCode, EstimatedDelivery│
├────────────────────────────────────────────────────────────┤
│ CancelOrderCommand : IRequest<CancellationResult>          │
│   - OrderId, Reason                                        │
│   - Returns: Success, RefundAmount (if applicable)         │
├────────────────────────────────────────────────────────────┤
│ UpdateOrderAddressCommand : IRequest<UpdateResult>         │
│   - OrderId, NewShippingAddress                            │
│   - Returns: Success, AffectedShipments                    │
└────────────────────────────────────────────────────────────┘

Queries (implement IRequest<TResponse>):
┌────────────────────────────────────────────────────────────┐
│ GetOrderByIdQuery : IRequest<OrderDto>                     │
│   - OrderId                                                │
│   - Returns: Full order details                            │
├────────────────────────────────────────────────────────────┤
│ GetOrderByTrackingQuery : IRequest<OrderDto>               │
│   - TrackingNumber                                         │
│   - Returns: Order associated with tracking                │
├────────────────────────────────────────────────────────────┤
│ ListOrdersQuery : IRequest<PagedResult<OrderSummaryDto>>   │
│   - CustomerId, Status, DateRange, Page, PageSize          │
│   - Returns: Paginated order list                          │
├────────────────────────────────────────────────────────────┤
│ SearchOrdersQuery : IRequest<List<OrderSummaryDto>>        │
│   - SearchTerm (order number, tracking, customer)          │
│   - Returns: Matching orders                               │
├────────────────────────────────────────────────────────────┤
│ GetOrderStatusQuery : IRequest<OrderStatusDto>             │
│   - OrderId or TrackingNumber                              │
│   - Returns: Current status with tracking info             │
└────────────────────────────────────────────────────────────┘
```

---

## 📋 Command & Query Catalog

### Commands (Write Operations)

| Command | Purpose | Handler Logic | Events Published |
|---------|---------|---------------|------------------|
| CreateOrderCommand | Create new order | Validate → Create → Save | OrderCreated |
| ConfirmOrderCommand | Confirm after payment | State.Confirm() | OrderConfirmed |
| DispatchOrderCommand | Book carrier, dispatch | Route → Quote → Book | OrderDispatched |
| CancelOrderCommand | Cancel order | State.Cancel() | OrderCancelled |
| UpdateAddressCommand | Change delivery address | Validate → Update | AddressUpdated |
| AddOrderItemCommand | Add item to order | Validate → Add → Recalc | ItemAdded |
| RemoveOrderItemCommand | Remove item | Validate → Remove → Recalc | ItemRemoved |
| AssignCarrierCommand | Manual carrier assignment | Validate → Assign | CarrierAssigned |
| **RequestReturnCommand** | Customer requests return | Validate → Create return | ReturnRequested |
| **ApproveReturnCommand** | CS approves return | Validate → Schedule pickup | ReturnApproved |
| **RejectReturnCommand** | CS rejects return | Validate → Notify customer | ReturnRejected |
| **ConfirmReturnReceiptCommand** | Warehouse confirms receipt | QC → Update inventory | ReturnReceived |
| **ProcessRefundCommand** | Process refund | Calculate → Call payment | RefundProcessed |
| **CreatePartialShipmentCommand** | Ship subset of items | Validate → Create shipment | PartialShipmentCreated |

### Queries (Read Operations)

| Query | Purpose | Data Source | Caching |
|-------|---------|-------------|---------|
| GetOrderByIdQuery | Single order details | Orders table | 5 min |
| GetOrderByNumberQuery | Lookup by order number | Orders table | 5 min |
| ListOrdersQuery | Paginated list | Orders + Joins | 1 min |
| SearchOrdersQuery | Full-text search | Search index | No |
| GetOrderStatusQuery | Status + tracking | Orders + Tracking | 30 sec |
| GetOrderHistoryQuery | Audit trail | Events table | 10 min |
| GetDashboardStatsQuery | Summary metrics | Aggregated view | 5 min |

---

## 📊 Data Models

### Order (Aggregate Root)

| Property | Type | Description |
|----------|------|-------------|
| Id | Guid | Primary identifier |
| OrderNumber | string | Human-readable (ORD-20260131-0001) |
| CustomerId | Guid | Customer reference |
| Customer | CustomerInfo (VO) | Embedded customer details |
| ShippingAddress | Address (VO) | Delivery address |
| BillingAddress | Address (VO) | Invoice address |
| Status | OrderStatus | Current state (enum) |
| State | IOrderState | State pattern implementation |
| Items | List<OrderItem> | Line items |
| TotalAmount | Money (VO) | Order total |
| ServiceLevel | ServiceLevel | Express/Standard/Economy |
| ShipmentId | Guid? | Linked shipment |
| TrackingNumber | string? | Carrier tracking |
| OrderDate | DateTime | Creation timestamp |
| RequiredDelivery | DateTime? | Requested delivery date |
| Notes | string | Special instructions |
| Metadata | Dictionary | Custom fields |

### OrderItem (Entity)

| Property | Type | Description |
|----------|------|-------------|
| Id | Guid | Item identifier |
| ProductId | Guid | Product reference |
| ProductName | string | Display name |
| Quantity | int | Number of units |
| UnitPrice | Money | Price per unit |
| Weight | decimal | Weight in kg |
| Dimensions | Dimensions | L×W×H in cm |

### OrderStatus (Enum)

| Value | Name | Description | Cancellable | Modifiable | Flow |
|-------|------|-------------|-------------|------------|------|
| 0 | CREATED | Order created | ✅ | ✅ | Forward |
| 1 | CONFIRMED | Payment received | ✅ | ✅ | Forward |
| 2 | DISPATCHED | Carrier booked | ✅ | ⬜ | Forward |
| 3 | IN_TRANSIT | Carrier picked up | ⬜ | ⬜ | Forward |
| 4 | OUT_FOR_DELIVERY | Last mile delivery | ⬜ | ⬜ | Forward |
| 5 | DELIVERED | Successfully delivered | ⬜ | ⬜ | Forward |
| 6 | FAILED_DELIVERY | Delivery attempt failed | ⬜ | ⬜ | Forward |
| 7 | CANCELLED | Order cancelled | ⬜ | ⬜ | Terminal |
| 8 | EXCEPTION | Problem detected | ⬜ | ⬜ | Forward |
| 9 | PARTIALLY_SHIPPED | Some items shipped (多仓发货) | ⬜ | ⬜ | Forward |
| 10 | RETURN_REQUESTED | Customer requests return (退货申请) | ⬜ | ⬜ | Reverse |
| 11 | RETURN_IN_TRANSIT | Return shipment moving (退货运输中) | ⬜ | ⬜ | Reverse |
| 12 | RETURN_RECEIVED | Warehouse received return (退货已签收) | ⬜ | ⬜ | Reverse |
| 13 | REFUNDED | Refund processed (已退款) | ⬜ | ⬜ | Terminal |

### Domain Events

| Event | Trigger | Payload | Subscribers | Flow |
|-------|---------|---------|-------------|------|
| OrderCreated | New order | OrderId, Customer, Items | Inventory, Notification | Forward |
| OrderConfirmed | Payment | OrderId, Amount | Dispatch service | Forward |
| OrderDispatched | Carrier booked | OrderId, TrackingNo | Tracking, Customer | Forward |
| OrderDelivered | Delivery confirmed | OrderId, DeliveredAt | Customer, Analytics | Forward |
| OrderCancelled | Cancellation | OrderId, Reason | Inventory, Refund | Terminal |
| OrderException | Problem | OrderId, ExceptionType | Operations | Forward |
| PartialShipmentCreated | Split shipment | OrderId, ShipmentId, Items | Tracking, Customer | Forward |
| **ReturnRequested** | Customer initiates | OrderId, Reason, Items | CS, Warehouse | Reverse |
| **ReturnApproved** | CS approves | OrderId, PickupDate | Logistics, Customer | Reverse |
| **ReturnRejected** | CS rejects | OrderId, RejectionReason | Customer | Reverse |
| **ReturnPickedUp** | Carrier pickup | OrderId, ReturnTrackingNo | Tracking | Reverse |
| **ReturnReceived** | Warehouse scan | OrderId, QCResult | Finance, Inventory | Reverse |
| **RefundProcessed** | Payment complete | OrderId, Amount, Method | Customer, Analytics | Terminal |

---

## 🔌 Integration Points

### Upstream Dependencies (Consumes)

| System | Data Consumed | Integration |
|--------|---------------|-------------|
| Dynamic Routing (01) | Route for dispatch | IRoutingService |
| Multi-Carrier (02) | Carrier quotes, booking | ICarrierService |
| Real-time Tracking (03) | Status updates | Domain events |
| Payment Service | Payment confirmation | Webhook/Event |

### Downstream Consumers (Produces)

| System | Data Provided | Integration |
|--------|---------------|-------------|
| Audit Tracking (05) | All order events | Domain events |
| Notification Service | Order updates | Domain events |
| Analytics | Order metrics | Event stream |
| Customer App | Order status | Query API |

### Event Flow

```
Order Created
     │
     ├──▶ Inventory Service (reserve stock)
     ├──▶ Notification Service (confirmation email)
     └──▶ Audit Service (log creation)

Order Dispatched  
     │
     ├──▶ Tracking Service (start tracking)
     ├──▶ Notification Service (shipped email)
     └──▶ Audit Service (log dispatch)

Order Delivered
     │
     ├──▶ Notification Service (delivery confirmation)
     ├──▶ Analytics Service (update metrics)
     └──▶ Audit Service (log delivery)
```

---

## 📚 Study Resources

### Chinese Tech Community References

| Source | Search Keywords | Focus |
|--------|-----------------|-------|
| CSDN | `运单状态 CQRS 实战` | CQRS for logistics |
| CSDN | `智能分单 CQRS` | Order dispatch |
| CSDN | `OMS系统 订单处理` | OMS architecture |
| Gitee | `DDD-CQRS-ES-Sample` | Full CQRS example |
| 掘金 | `中通物流运单流转` | ZTO order processing |

---

## 🇨🇳 Chinese Industry Practices

### Leading Logistics Company Comparison (头部物流企业对比)

| Aspect | 京东物流 (JD Logistics) | 顺丰速运 (SF Express) | 中通快递 (ZTO Express) |
|--------|------------------------|----------------------|------------------------|
| **Architecture Style** | 一体化集成 (Integrated) | 智慧大脑 (Smart Brain) | 高扩展网络 (Scalable Network) |
| **State Machine** | 50+ states (精细化) | 30+ states (标准化) | 25+ states (简洁化) |
| **CQRS Adoption** | Full CQRS + Event Sourcing | CQRS for high-volume | Partial CQRS |
| **Dispatch Algorithm** | AI-driven (达达配送) | Rule-based + ML | Distance-first |
| **Peak Capacity** | 10M+ orders/day | 8M+ orders/day | 15M+ orders/day |
| **Unique Feature** | 仓配一体化 | 时效承诺 | 加盟网络 |

### JD Logistics Architecture Insights (京东物流架构参考)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    京东物流订单处理架构 (JD OMS Architecture)                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Key Characteristics:                                                       │
│                                                                             │
│  1. 仓配一体化 (Warehouse-Delivery Integration)                              │
│     └── Order → Warehouse Selection → Pick-Pack → Carrier → Delivery        │
│         State machine spans both warehouse and delivery operations          │
│                                                                             │
│  2. 青龙系统 (Qinglong System)                                               │
│     └── Centralized order orchestration                                     │
│     └── Real-time inventory visibility                                      │
│     └── Dynamic warehouse selection based on inventory + proximity          │
│                                                                             │
│  3. 达达配送集成 (Dada Express Integration)                                  │
│     └── Same-day/Next-hour delivery orders                                  │
│     └── Crowd-sourced delivery workforce                                    │
│     └── Real-time demand-supply matching                                    │
│                                                                             │
│  4. 状态精细化管理 (Fine-grained State Management)                           │
│     └── Warehouse states: ALLOCATED → PICKING → PICKED → PACKED → HANDED_OFF│
│     └── Transit states: PICKED_UP → HUB_1 → HUB_2 → ... → OUT_FOR_DELIVERY  │
│     └── Return states: RETURN_CREATED → RETURN_APPROVED → RETURN_PICKED_UP  │
│                                                                             │
│  Learnings for DT-Express:                                                  │
│  ✅ Consider fine-grained warehouse states if WMS integration needed        │
│  ✅ PARTIALLY_SHIPPED state essential for multi-warehouse fulfillment       │
│  ✅ Return flow should mirror forward flow complexity                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

### SF Express Smart Brain Reference (顺丰智慧大脑参考)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    顺丰智慧大脑分单系统 (SF Smart Dispatch)                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Key Characteristics:                                                        │
│                                                                              │
│  1. 时效承诺 (Time Commitment)                                               │
│     └── "次日达"、"隔日达"、"即日达" service levels                           │
│     └── Dispatch algorithm optimizes for committed time                      │
│     └── Dynamic routing adjusts in real-time                                 │
│                                                                              │
│  2. 网点智能调度 (Outlet Smart Scheduling)                                    │
│     └── 1800+ outlets with real-time capacity visibility                    │
│     └── Load balancing across outlets                                        │
│     └── Courier assignment optimization                                      │
│                                                                              │
│  3. 大数据预测 (Big Data Prediction)                                         │
│     └── Volume prediction 24-48 hours ahead                                  │
│     └── Pre-positioning of resources                                         │
│     └── Dynamic pricing based on predicted demand                            │
│                                                                              │
│  4. 异常预警 (Exception Alerting)                                            │
│     └── Proactive exception detection                                        │
│     └── Auto-escalation rules                                                │
│     └── Customer notification before impact                                  │
│                                                                              │
│  Learnings for DT-Express:                                                   │
│  ✅ Time commitment as a first-class concept in dispatch                    │
│  ✅ Capacity scoring weight should increase during peaks                    │
│  ✅ Exception state should trigger proactive notifications                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### ZTO Network Architecture Reference (中通网络架构参考)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    中通快递网络架构 (ZTO Network Design)                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Key Characteristics:                                                       │
│                                                                             │
│  1. 加盟网络模式 (Franchise Network Model)                                   │
│     └── 30,000+ franchise outlets                                           │
│     └── Standardized but flexible state machine                             │
│     └── Outlet-level customization allowed                                  │
│                                                                             │
│  2. 高吞吐量设计 (High Throughput Design)                                    │
│     └── 15M+ orders/day during Double 11                                    │
│     └── Eventually consistent read models                                   │
│     └── Aggressive caching strategies                                       │
│                                                                             │
│  3. 成本优先分单 (Cost-First Dispatch)                                       │
│     └── Cost scoring has highest weight (0.40)                              │
│     └── Aggregation for better rates                                        │
│     └── Dynamic route optimization                                          │
│                                                                             │
│  4. 简洁状态设计 (Simplified State Design)                                   │
│     └── Fewer states, more metadata                                         │
│     └── State + SubStatus pattern                                           │
│     └── Easier franchise training                                           │
│                                                                             │
│  Learnings for DT-Express:                                                  │
│  ✅ Consider State + SubStatus for flexibility without complexity           │
│  ✅ Cost optimization critical for price-sensitive markets                  │
│  ✅ Read model caching essential for high throughput                        │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📋 Design Pattern Ledger

### Patterns Used in Order Processing Domain (本域设计模式登记簿)

| Pattern | Location | Purpose | Study Guide |
|---------|----------|---------|-------------|
| **State Pattern** (状态模式) | Order lifecycle management | Encapsulate state-specific behavior, prevent invalid transitions | [STATE-PATTERN.md](../design-patterns/STATE-PATTERN.md) |
| **CQRS Pattern** (命令查询分离) | Read/Write separation | Optimize reads and writes independently | [CQRS-PATTERN.md](../design-patterns/CQRS-PATTERN.md) |
| **Strategy Pattern** (策略模式) | Dispatch scoring strategies | Interchangeable scoring algorithms | [STRATEGY-PATTERN.md](../design-patterns/STRATEGY-PATTERN.md) |
| **Factory Pattern** (工厂模式) | Order creation from channels | Create orders from different sources | [FACTORY-PATTERN.md](../design-patterns/FACTORY-PATTERN.md) |
| **Specification Pattern** (规格模式) | Business rule filtering | Composable business rules | ABP docs |
| **Mediator Pattern** (中介者模式) | Command/Query handling | Decouple handlers from controllers | MediatR docs |
| **Template Method** (模板方法) | Scoring calculation flow | Define scoring algorithm skeleton | Refactoring Guru |
| **Chain of Responsibility** (责任链) | Rule engine filtering | Sequential rule evaluation | Refactoring Guru |
| **Event Sourcing** (事件溯源) | Order history/audit | Store all changes as events | Microsoft docs |
| **Repository Pattern** (仓储模式) | Data access abstraction | Abstract persistence layer | ABP docs |

### Pattern Decision Matrix (模式选型决策矩阵)

| Problem | Considered Patterns | Chosen | Reason |
|---------|---------------------|--------|--------|
| Order lifecycle | State vs Workflow Engine | **State Pattern** | Simpler, code-based, type-safe |
| Read/Write optimization | Single model vs CQRS | **CQRS** | Different scaling needs |
| Carrier scoring | If-else vs Strategy | **Strategy Pattern** | Configurable, extensible |
| Order creation | Constructor vs Factory | **Factory Pattern** | Multiple creation sources |
| Rule evaluation | Hard-coded vs Rules Engine | **Rules Engine + Specification** | Business user configurable |
| Handler routing | Direct call vs Mediator | **Mediator (MediatR)** | Decoupling, pipeline behaviors |

### Pattern Interaction Diagram (模式交互图)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    PATTERN COLLABORATION (模式协作)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                         API Request                                         │
│                              │                                              │
│                              ▼                                              │
│                    ┌─────────────────┐                                      │
│                    │ Factory Pattern │ ← Create order from channel          │
│                    │ (OrderFactory)  │                                      │
│                    └────────┬────────┘                                      │
│                             │                                               │
│                             ▼                                               │
│                    ┌─────────────────┐                                      │
│                    │ Mediator Pattern│ ← Route to handler                   │
│                    │ (MediatR)       │                                      │
│                    └────────┬────────┘                                      │
│                             │                                               │
│              ┌──────────────┼──────────────┐                                │
│              │              │              │                                │
│              ▼              ▼              ▼                                │
│     ┌──────────────┐ ┌─────────────┐ ┌──────────────┐                       │
│     │ State Pattern│ │   CQRS     │ │Specification │                        │
│     │ (Lifecycle)  │ │ (Read/Write)│ │ (Validation) │                       │
│     └──────┬───────┘ └─────┬───────┘ └──────┬───────┘                       │
│            │               │                │                               │
│            └───────────────┼────────────────┘                               │
│                            │                                                │
│                            ▼                                                │
│                    ┌─────────────────┐                                      │
│                    │Strategy Pattern │ ← Score carriers                     │
│                    │(Dispatch Scoring)│                                     │
│                    └────────┬────────┘                                      │
│                             │                                               │
│                             ▼                                               │
│                    ┌─────────────────┐                                      │
│                    │ Chain of Resp.  │ ← Filter by rules                    │
│                    │ (Rules Engine)  │                                      │
│                    └────────┬────────┘                                      │
│                             │                                               │
│                             ▼                                               │
│                    ┌─────────────────┐                                      │
│                    │ Event Sourcing  │ ← Record all changes                 │
│                    │ (Audit Trail)   │                                      │
│                    └─────────────────┘                                      │
└─────────────────────────────────────────────────────────────────────────────┘
```

### ABP Framework Reference

| Resource | Content |
|----------|---------|
| ABP CQRS Module | github.com/abpframework |
| ABP State Machine | State management patterns |

### Design Pattern References

| Resource | Content | Application |
|----------|---------|-------------|
| Refactoring Guru - State | refactoring.guru/state | Order lifecycle |
| Microsoft CQRS | docs.microsoft.com/cqrs | CQRS implementation |
| MediatR Docs | github.com/jbogard/MediatR | Command/Query handling |

---

## ✅ Acceptance Criteria

### Functional Acceptance

| ID | Criteria | Test Method |
|----|----------|-------------|
| AC-OR-001 | Can create order via API | Integration test |
| AC-OR-002 | Can create order via web form | UI test |
| AC-OR-003 | Duplicate orders are rejected | Unit test |
| AC-OR-004 | Order state transitions correctly | State machine test |
| AC-OR-005 | Invalid state transitions throw error | Unit test |
| AC-OR-006 | Order can be cancelled before pickup | Integration test |
| AC-OR-007 | Order cannot be cancelled after pickup | Unit test |
| AC-OR-008 | Dispatch triggers carrier booking | Integration test |
| AC-OR-009 | Tracking updates update order status | Event test |
| AC-OR-010 | Query returns order within 100ms | Performance test |

### Non-Functional Acceptance

| ID | Criteria | Target | Test Method |
|----|----------|--------|-------------|
| NFR-OR-001 | Order creation time | < 500ms | Performance |
| NFR-OR-002 | Query response time | < 100ms | Performance |
| NFR-OR-003 | Concurrent order creation | 200/sec | Load test |
| NFR-OR-004 | State transition time | < 50ms | Performance |
| NFR-OR-005 | Event publishing | < 100ms | Performance |

---

## 🔗 Related Documents

- **Uses**: [01-DYNAMIC-ROUTING.md](01-DYNAMIC-ROUTING.md) - For route calculation
- **Uses**: [02-MULTI-CARRIER.md](02-MULTI-CARRIER.md) - For carrier booking
- **Uses**: [03-REALTIME-TRACKING.md](03-REALTIME-TRACKING.md) - For status updates
- **Next**: [05-AUDIT-TRACKING.md](05-AUDIT-TRACKING.md) - Logs all order events
- **Index**: [00-INDEX.md](../00-INDEX.md)

---

*Enhanced: Reverse logistics states, Deep CQRS, Smart dispatch algorithm, Chinese industry practices*
