# 📦 04-ORDER-PROCESSING — Multi-Pattern Design Spec

> **Domain**: Order Processing — OMS orchestration inside a TMS  
> **Patterns**: State · CQRS · (Optional) Strategy (dispatch policy) · (Optional) Specification (validation)  
> **Goal**: Interview-ready, production-oriented (CN carriers + reverse logistics) without over-engineering  
> **Dependencies**: [01-DYNAMIC-ROUTING.md](01-DYNAMIC-ROUTING.md) · [02-MULTI-CARRIER.md](02-MULTI-CARRIER.md) · [03-REALTIME-TRACKING.md](03-REALTIME-TRACKING.md) · [05-AUDIT-TRACKING.md](05-AUDIT-TRACKING.md)

---

## 📋 Table of Contents

1. [Domain Overview](#-domain-overview)
2. [Business Context](#-business-context)
3. [Architecture Layers](#-architecture-layers)
4. [Feature Specification](#-feature-specification)
5. [Design Pattern Application](#-design-pattern-application)
6. [State Machine Design](#-state-machine-design)
7. [CQRS Design](#-cqrs-design)
8. [Interface Contracts](#-interface-contracts)
9. [Command & Query Catalog](#-command--query-catalog)
10. [Data Models](#-data-models)
11. [Integration Points](#-integration-points)
12. [Acceptance Criteria](#-acceptance-criteria)
13. [Project Structure](#-project-structure)
14. [Interview Guide](#-interview-guide)
15. [Study Resources](#-study-resources)
16. [Related Documents](#-related-documents)

---

## 🎯 Domain Overview

### Elevator Pitch

> “Order Processing is the **orchestration center** of the TMS: it validates order intent, manages the order lifecycle via a **State Machine**, and coordinates routing + carrier booking + tracking. **CQRS** keeps write-side invariants strict while enabling fast read models for customer support and dashboards. Reverse logistics (退货/退款) is modeled as first-class states, not ad-hoc flags.”

### Purpose

- Create and validate orders from multiple channels (API/Web/Import)
- Enforce lifecycle correctness (no invalid transitions)
- Dispatch orders by coordinating:
  - routing constraints (01)
  - carrier quote/booking (02)
  - tracking updates consumption (03)
- Support reverse logistics (returns/refunds) as a controlled flow

### Scope

| In Scope | Out of Scope |
|----------|--------------|
| Order creation + validation + idempotency | Customer/CRM management |
| State machine for forward + reverse flows | WMS deep operations (inventory picking) |
| CQRS-style commands/queries + read models | Finance settlement beyond “refund requested/processed” |
| Dispatch orchestration (calls into 01 + 02) | Building the real carrier adapters (see 02) |
| Integration events for audit and tracking | Full event sourcing implementation |

### Key Insight

- **State Pattern is the correctness backbone**: transitions encode rules like “cancel only before pickup”.
- **CQRS is the performance/UX backbone**: reads are optimized views; writes protect invariants.
- **China carrier realism stays at the boundary**: the OMS uses ports; carriers (顺丰/京东/中通/圆通/申通…) are resolved by 02.

---

## 💼 Business Context

### Typical Order Types (examples)

| Type | Source | Notes | Special Handling |
|------|--------|------|------------------|
| E-commerce | API/Web/App | high volume | auto-dispatch |
| B2B | API/EDI | scheduled pickups | batch + SLA |
| Walk-in | counter | immediate | manual confirmation |
| Return (reverse) | App/Web/CS | return/refund | reverse flow states |
| International | API | customs docs | multi-leg, longer SLA |

### Roles

- **Customer**: create/cancel/request return
- **Dispatcher**: manual override on dispatch (exceptions)
- **CS / Customer Support**: approve/reject return, view timeline
- **System**: auto-dispatch, SLA jobs, read-model projection

### Business Rules (minimal, production-flavored)

| Rule ID | Rule | Where enforced |
|---------|------|----------------|
| BR-OR-001 | Order must have valid addresses | command validation + address service port |
| BR-OR-002 | Order cannot exceed max weight (30kg standard) | command validation (Specification) |
| BR-OR-003 | Express orders dispatch within 30 minutes | SLA monitor + alert |
| BR-OR-004 | Order can only cancel before pickup | state guard (State Pattern) |
| BR-OR-005 | Duplicate orders rejected within 5 minutes | idempotency key + request log |
| BR-OR-006 | COD orders require customer verification | workflow step on confirm |
| BR-OR-007 | Auto-cancel after 24h without payment | scheduled job (command) |
| BR-OR-008 | Returns must be requested within policy window (e.g., 7 days) | reverse-flow state guard |
| BR-OR-009 | Refund amount depends on QC result (full/partial/deny) | reverse-flow command + state guard |

### Core Use Cases (CQRS framing)

#### UC-OR-001: Create Order (Command)

- Validate request (address, weight, idempotency)
- Create `Order` in `CREATED`
- Emit `OrderCreated`

#### UC-OR-002: Confirm Payment / Confirm Order (Command)

- Ensure current state allows confirm
- If COD: ensure verification step is satisfied
- Transition `CREATED → CONFIRMED`
- Emit `OrderConfirmed`

#### UC-OR-003: Dispatch Order (Command)

- Call routing (01) for constraints/plan
- Call multi-carrier quotes + booking (02)
- Transition `CONFIRMED → DISPATCHED`
- Persist shipment reference + tracking number
- Emit `OrderDispatched`

#### UC-OR-004: View Order Status (Query)

- Read `OrderDetailView` from read store
- Optionally merge “latest tracking snapshot” from 03

#### UC-OR-005: Cancel Order (Command)

- Guard: only before pickup
- If shipment booked: request cancel via 02
- Transition to `CANCELLED`
- Emit `OrderCancelled`

#### UC-OR-006: Request Return (Command)

- Guard: only after `DELIVERED`, within policy window
- Transition to `RETURN_REQUESTED`
- Emit `ReturnRequested`

#### UC-OR-007: Process Refund (Command)

- Guard: return received + QC result available
- Transition to `REFUNDED`
- Emit `RefundProcessed`

---

## 🏗 Architecture Layers

### Separation Principle (production but minimal)

```
┌──────────────────────────────────────────────────────────────┐
│                         API Layer                            │
│  Controllers / Endpoints (Commands + Queries)                │
└──────────────────────────────┬───────────────────────────────┘
                               │
┌──────────────────────────────▼───────────────────────────────┐
│                      Application Layer                       │
│  • Command Handlers (validate → domain → persist → events)    │
│  • Query Handlers (read optimized views)                       │
│  • Ports: IRoutingPort, ICarrierPort, ITrackingPort            │
└──────────────────────────────┬───────────────────────────────┘
                               │ depends on
┌──────────────────────────────▼───────────────────────────────┐
│                         Domain Layer                          │
│  • Order Aggregate (invariants + domain events)                │
│  • State Machine (IOrderState, guards)                         │
│  • Value Objects (Weight, Money, Address)                      │
└──────────────────────────────┬───────────────────────────────┘
                               │ implemented by
┌──────────────────────────────▼───────────────────────────────┐
│                    Infrastructure Layer                        │
│  • Repository (write model)                                    │
│  • Read model projector / updater                              │
│  • Message bus adapter (optional)                              │
│  • Integrations: 01/02/03 via their ports                       │
└──────────────────────────────────────────────────────────────┘
```

### Why this matters

- Keeps domain rules testable without carrier/routing SDKs.
- Allows “start simple” (single DB + in-process projection) and evolve to async projection later.

---

## 📝 Feature Specification

| # | Feature | Priority | Pattern(s) | Notes |
|---|---------|----------|------------|------|
| OR-F001 | Multi-channel order creation | High | Factory + CQRS | API/Web/Import create commands |
| OR-F002 | Validation + invariants | High | Specification + State | e.g., weight ≤ 30kg, cancel window |
| OR-F003 | Lifecycle management | High | State | explicit transitions + guards |
| OR-F004 | Dispatch orchestration (CN carriers via 02) | High | Strategy (optional) | carrier selection policy is pluggable |
| OR-F005 | Fast reads: list/detail/timeline | High | CQRS | read models optimized for CS/Ops |
| OR-F006 | Reverse logistics (return/refund) | High | State + CQRS | return is a controlled flow |
| OR-F007 | Idempotency + duplicate prevention | High | CQRS | protects high-volume create endpoints |

---

## 🧩 Design Pattern Application

### State Pattern (生命周期正确性)

- `Order` holds current `IOrderState`.
- Each state allows only valid actions.
- Guards encode rules (cancel window, return window, etc.).

### CQRS (读写分离)

- **Commands** mutate the write model, enforce invariants, emit events.
- **Queries** read from optimized projections (can start with same DB views; evolve to separate store).

### Optional Strategy (Dispatch Policy)

- Keep “how to choose carrier” separate from order lifecycle.
- Example scoring dimensions (production-flavored, minimal): cost, SLA, capacity, destination coverage.

### Optional Specification (Validation)

- Encapsulate validation rules so they remain composable and testable:
  - `MaxWeightSpec(30kg)`
  - `AddressCompleteSpec`
  - `ReturnWindowSpec(7days)`

---

## 🎨 State Machine Design

### Forward Flow States

`CREATED → CONFIRMED → DISPATCHED → IN_TRANSIT → OUT_FOR_DELIVERY → DELIVERED`

Additional forward states (kept minimal):

- `FAILED_DELIVERY` (retry window)
- `CANCELLED` (terminal)
- `EXCEPTION` (entered when a problem occurs; requires manual resolution)

### Reverse Logistics States (逆向物流)

`DELIVERED → RETURN_REQUESTED → RETURN_IN_TRANSIT → RETURN_RECEIVED → REFUNDED`

### Minimal Transition Table

| From | To | Trigger | Guard (examples) |
|------|----|---------|------------------|
| CREATED | CONFIRMED | ConfirmPayment | payment ok / COD verified |
| CREATED | CANCELLED | Cancel | within cancel window |
| CONFIRMED | DISPATCHED | Dispatch | carrier booked + tracking assigned |
| DISPATCHED | IN_TRANSIT | CarrierPickup | tracking says picked up |
| OUT_FOR_DELIVERY | DELIVERED | ConfirmDelivery | proof captured |
| OUT_FOR_DELIVERY | FAILED_DELIVERY | DeliveryFailed | reason recorded |
| DELIVERED | RETURN_REQUESTED | RequestReturn | within return policy window |
| RETURN_IN_TRANSIT | RETURN_RECEIVED | WarehouseReceipt | QC initiated |
| RETURN_RECEIVED | REFUNDED | ProcessRefund | QC passed / partial policy |

### Visual Sketch (kept compact)

```
CREATED ──confirm──▶ CONFIRMED ──dispatch──▶ DISPATCHED ──pickup──▶ IN_TRANSIT ──▶ ... ──▶ DELIVERED
   │                     │
 cancel                  │
   ▼                     ▼
CANCELLED            EXCEPTION (manual resolution)

DELIVERED ──request return──▶ RETURN_REQUESTED ──pickup──▶ RETURN_IN_TRANSIT ──receipt/QC──▶ RETURN_RECEIVED ──refund──▶ REFUNDED
```

---

## 🔄 CQRS Design

### Write Side (Commands)

- Transaction boundary: one order aggregate per command
- Persist order + append audit record + emit domain events
- Use idempotency key for high-volume endpoints (create, dispatch)

### Read Side (Queries)

- Read models are shaped for views:
  - list (fast filters)
  - detail (single fetch)
  - timeline (audit-friendly)

### Projection Strategy (pragmatic)

Start simple:

- Same DB, projection tables updated in-process after commit

Evolve when needed:

- Event-driven projection (queue) with eventual consistency window (e.g., 50–200ms)

---

## 🤝 Interface Contracts

### Ports (cross-domain boundaries)

- `IRoutingPort` (calls 01): request route constraints/plan for dispatch
- `ICarrierPort` (calls 02): quote + book + cancel shipment; returns `TrackingNumber`
- `ITrackingPort` (calls 03): get latest tracking snapshot by tracking number
- `IAuditPort` (calls 05): write append-only audit/timeline entries

### Core persistence + messaging

- `IOrderRepository` (write model): `Get`, `Save`, optimistic concurrency
- `IOrderReadService` (read model): list/search/detail queries
- `IEventPublisher` (domain/integration events): `Publish(events)`
- `IClock` / `IIdGenerator` (testable time/IDs)

---

## 📚 Command & Query Catalog

### Commands (Write)

| Command | Intent | Key Output |
|--------|--------|------------|
| `CreateOrder` | create order in `CREATED` | `OrderId`, `OrderNumber` |
| `ConfirmPayment` | `CREATED → CONFIRMED` | updated state |
| `DispatchOrder` | orchestrate 01 + 02, assign tracking | `TrackingNumber` |
| `CancelOrder` | cancel if allowed; call 02 if booked | updated state |
| `MarkCarrierPickedUp` | `DISPATCHED → IN_TRANSIT` | updated state |
| `ConfirmDelivery` | mark delivered | updated state |
| `RequestReturn` | start reverse flow | return case id |
| `ApproveReturnPickup` | schedule reverse shipment via 02 | reverse tracking |
| `MarkReturnReceived` | warehouse receipt + QC summary | updated state |
| `ProcessRefund` | call payment/refund port, terminal | refund reference |

### Queries (Read)

| Query | View |
|------|------|
| `GetOrderById` | detail view |
| `GetOrderByNumber` | detail view |
| `SearchOrders` | list view (filters: status, date, phone mask) |
| `GetOrderTimeline` | timeline/audit view |
| `GetOrderStatus` | “small payload” status endpoint |

---

## 🧱 Data Models

### Write Model (Domain)

- `Order` (Aggregate)
  - `OrderId`, `OrderNumber`
  - `SenderAddress`, `ReceiverAddress`
  - `Items[]`, `TotalWeight`, `TotalAmount`
  - `Status` (driven by State machine)
  - `Shipments[]` (supports split shipment if needed, kept optional)
  - `ReturnCase?` (only after delivered)

### Value Objects

- `Weight` (grams/kg, with max enforcement)
- `Money` (amount + currency)
- `Address` (structured fields)

### Read Models (CQRS)

- `OrderListView`: indexed fields for fast paging/search
- `OrderDetailView`: denormalized JSON-like view for CS
- `OrderTimelineView`: append-only rows (`time`, `event`, `operator`, `detail`)

---

## 🔌 Integration Points

### With other domains

- 01 routing: constraints + plan for dispatch decisions
- 02 multi-carrier: CN-carrier booking abstraction (顺丰/京东/中通/圆通/申通…)
- 03 realtime tracking: consume `TrackingStatusChanged` to advance state where appropriate
- 05 audit tracking: persist timeline/audit events for compliance and debugging

### External/adjacent services (kept minimal)

- Payment/refund service: invoked on `ConfirmPayment`, `ProcessRefund`
- Notification: send customer updates on major transitions

### Legacy compatibility (practical, not bloated)

- Provide a thin adapter to translate legacy “status callback” into a command (e.g., `MarkCarrierPickedUp`, `ConfirmDelivery`).
- Keep legacy payload mapping out of the domain (same philosophy as 02/03).

---

## ✅ Acceptance Criteria

### Functional

- Order creation rejects duplicates within 5 minutes (idempotency key).
- BR-OR-002 enforced: orders over 30kg are rejected.
- Cancel is blocked after pickup (state guard).
- Dispatch coordinates 01 + 02 and stores the returned tracking number.
- Return/refund flow is explicit: cannot refund without return receipt + QC result.
- Queries return list/detail/timeline without running domain logic.

### Non-functional (reasonable targets)

- Command handlers are deterministic and testable (no carrier SDK calls inside domain layer).
- Read endpoints are optimized (target: p95 list/detail reads < 200ms with caching/projection).
- State transitions are auditable (timeline entries are written for each transition).

### Testing Checklist

- Unit tests: state transitions (allowed + disallowed), guards (cancel window, return window).
- Unit tests: validation specifications (max weight, address completeness).
- Integration tests (contract-level): 01/02/03 ports mocked; dispatch uses routing + booking.
- Read model tests: projection updated when `OrderStatusChanged` is emitted.

---

## 🗂 Project Structure

A typical clean structure (mirrors 01/02/03 style):

- `src/OrderProcessing.Core/`
  - domain models, state machine, value objects, domain events
- `src/OrderProcessing.Application/`
  - commands, queries, handlers, ports
- `src/OrderProcessing.Infrastructure/`
  - repositories, projection updater, adapters to 01/02/03/05
- `src/OrderProcessing.Demo/`
  - composition root + sample scenarios

---

## 🧠 Interview Guide

- Why State Pattern here instead of `switch(status)`?
- Where do you enforce BR-OR-002 (30kg max) and why?
- How does CQRS help in OMS workloads (CS dashboards vs write invariants)?
- What’s your consistency approach for read models (start simple → evolve)?
- How do you keep carrier-specific complexity out of OMS?
- How would you model returns without creating “status explosion”?

---

## 📚 Study Resources

- Patterns:
  - [../design-patterns/STATE-PATTERN.md](../design-patterns/STATE-PATTERN.md)
  - [../design-patterns/CQRS-PATTERN.md](../design-patterns/CQRS-PATTERN.md)
  - (Optional) [../design-patterns/STRATEGY-PATTERN.md](../design-patterns/STRATEGY-PATTERN.md)
- Data models:
  - [../data-models/ORDER-AGGREGATE.md](../data-models/ORDER-AGGREGATE.md)
  - [../data-models/VALUE-OBJECTS.md](../data-models/VALUE-OBJECTS.md)

---

## 🔗 Related Documents

- System:
  - [../00-INDEX.md](../00-INDEX.md)
  - [../01-SYSTEM-VISION.md](../01-SYSTEM-VISION.md)
- Core domains:
  - [01-DYNAMIC-ROUTING.md](01-DYNAMIC-ROUTING.md)
  - [02-MULTI-CARRIER.md](02-MULTI-CARRIER.md)
  - [03-REALTIME-TRACKING.md](03-REALTIME-TRACKING.md)
  - [05-AUDIT-TRACKING.md](05-AUDIT-TRACKING.md)
