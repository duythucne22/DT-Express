# 📝 07-IMPLEMENTATION-TASKS — Worker Agent Task Breakdown

> **Purpose**: Ordered, dependency-aware task list for implementation  
> **Rule**: Each task produces compilable code before moving to the next  
> **Rule**: Each task includes files to create, acceptance criteria, and estimated complexity  
> **Total Phases**: 7 · **Total Tasks**: 28

---

## 📋 Table of Contents

1. [Phase 0: Solution Scaffolding](#phase-0-solution-scaffolding)
2. [Phase 1: Domain Layer](#phase-1-domain-layer)
3. [Phase 2: Application Layer](#phase-2-application-layer)
4. [Phase 3: Infrastructure — Algorithms & Core](#phase-3-infrastructure--algorithms--core)
5. [Phase 4: Infrastructure — Patterns & Stores](#phase-4-infrastructure--patterns--stores)
6. [Phase 5: API Layer](#phase-5-api-layer)
7. [Phase 6: Tests](#phase-6-tests)
8. [Phase 7: Verification & Polish](#phase-7-verification--polish)
9. [Dependency Graph](#dependency-graph)

---

## Phase 0: Solution Scaffolding

### Task 0.1 — Create Solution and Projects

**Files to create:**
```
DtExpress.sln
src/DtExpress.Domain/DtExpress.Domain.csproj
src/DtExpress.Application/DtExpress.Application.csproj
src/DtExpress.Infrastructure/DtExpress.Infrastructure.csproj
src/DtExpress.Api/DtExpress.Api.csproj
tests/DtExpress.Domain.Tests/DtExpress.Domain.Tests.csproj
tests/DtExpress.Application.Tests/DtExpress.Application.Tests.csproj
tests/DtExpress.Infrastructure.Tests/DtExpress.Infrastructure.Tests.csproj
tests/DtExpress.Api.Tests/DtExpress.Api.Tests.csproj
```

**Commands:**
```bash
dotnet new sln -n DtExpress
dotnet new classlib -n DtExpress.Domain -o src/DtExpress.Domain
dotnet new classlib -n DtExpress.Application -o src/DtExpress.Application
dotnet new classlib -n DtExpress.Infrastructure -o src/DtExpress.Infrastructure
dotnet new webapi -n DtExpress.Api -o src/DtExpress.Api --no-openapi
dotnet new xunit -n DtExpress.Domain.Tests -o tests/DtExpress.Domain.Tests
dotnet new xunit -n DtExpress.Application.Tests -o tests/DtExpress.Application.Tests
dotnet new xunit -n DtExpress.Infrastructure.Tests -o tests/DtExpress.Infrastructure.Tests
dotnet new xunit -n DtExpress.Api.Tests -o tests/DtExpress.Api.Tests

# Add to solution
dotnet sln add src/DtExpress.Domain/
dotnet sln add src/DtExpress.Application/
dotnet sln add src/DtExpress.Infrastructure/
dotnet sln add src/DtExpress.Api/
dotnet sln add tests/DtExpress.Domain.Tests/
dotnet sln add tests/DtExpress.Application.Tests/
dotnet sln add tests/DtExpress.Infrastructure.Tests/
dotnet sln add tests/DtExpress.Api.Tests/

# Add project references (CRITICAL: enforces dependency rules)
dotnet add src/DtExpress.Application/ reference src/DtExpress.Domain/
dotnet add src/DtExpress.Infrastructure/ reference src/DtExpress.Application/
dotnet add src/DtExpress.Infrastructure/ reference src/DtExpress.Domain/
dotnet add src/DtExpress.Api/ reference src/DtExpress.Infrastructure/

# Test project references
dotnet add tests/DtExpress.Domain.Tests/ reference src/DtExpress.Domain/
dotnet add tests/DtExpress.Application.Tests/ reference src/DtExpress.Application/
dotnet add tests/DtExpress.Application.Tests/ reference src/DtExpress.Domain/
dotnet add tests/DtExpress.Infrastructure.Tests/ reference src/DtExpress.Infrastructure/
dotnet add tests/DtExpress.Infrastructure.Tests/ reference src/DtExpress.Application/
dotnet add tests/DtExpress.Infrastructure.Tests/ reference src/DtExpress.Domain/
dotnet add tests/DtExpress.Api.Tests/ reference src/DtExpress.Api/

# Add Swashbuckle to API only
dotnet add src/DtExpress.Api/ package Swashbuckle.AspNetCore

# Add testing package for API integration tests
dotnet add tests/DtExpress.Api.Tests/ package Microsoft.AspNetCore.Mvc.Testing
```

**Acceptance criteria:**
- [ ] `dotnet build` succeeds
- [ ] Domain `.csproj` has ZERO `<ProjectReference>` and ZERO `<PackageReference>`
- [ ] Application `.csproj` references ONLY Domain
- [ ] Infrastructure `.csproj` references Application + Domain
- [ ] Api `.csproj` references Infrastructure + has Swashbuckle

**Complexity:** Low  
**Dependencies:** None

---

## Phase 1: Domain Layer

> All files in `src/DtExpress.Domain/`. Must compile with zero external references.

### Task 1.1 — Common Abstractions + Domain Exception

**Files:**
- `Common/IClock.cs`
- `Common/IIdGenerator.cs`
- `Common/ICorrelationIdProvider.cs`
- `Common/DomainException.cs` (+ `InvalidStateTransitionException`, `CarrierNotFoundException`, `StrategyNotFoundException`)

**Acceptance:** `dotnet build src/DtExpress.Domain/` succeeds  
**Complexity:** Low  
**Dependencies:** Task 0.1

### Task 1.2 — Value Objects

**Files:**
- `ValueObjects/Address.cs`
- `ValueObjects/GeoCoordinate.cs`
- `ValueObjects/Money.cs`
- `ValueObjects/Weight.cs`
- `ValueObjects/Dimension.cs`
- `ValueObjects/ContactInfo.cs`
- `ValueObjects/TrackingNumber.cs`
- `ValueObjects/WeightUnit.cs` (enum)

**Acceptance:** All value objects have invariant enforcement in constructors  
**Complexity:** Medium (careful with equality, immutability)  
**Dependencies:** Task 1.1

### Task 1.3 — Routing Domain (Interfaces + Models + Enums)

**Files:**
- `Routing/Enums/ServiceLevel.cs`
- `Routing/Enums/RouteOptimization.cs`
- `Routing/Models/RouteRequest.cs`
- `Routing/Models/Route.cs`
- `Routing/Models/Graph.cs`
- `Routing/Models/GraphNode.cs`
- `Routing/Models/GraphEdge.cs`
- `Routing/Models/PathResult.cs`
- `Routing/Interfaces/IRouteStrategy.cs`
- `Routing/Interfaces/IPathfinder.cs`
- `Routing/Interfaces/IRouteStrategyFactory.cs`
- `Routing/Interfaces/IMapService.cs`

**Acceptance:** All interfaces have ≤3 methods  
**Complexity:** Medium  
**Dependencies:** Task 1.2

### Task 1.4 — Carrier Domain (Interfaces + Models + Enums)

**Files:**
- `Carrier/Enums/ShipmentStatus.cs`
- `Carrier/Models/QuoteRequest.cs`
- `Carrier/Models/CarrierQuote.cs`
- `Carrier/Models/BookingRequest.cs`
- `Carrier/Models/BookingResult.cs`
- `Carrier/Models/CarrierTrackingInfo.cs`
- `Carrier/Interfaces/ICarrierAdapter.cs`
- `Carrier/Interfaces/ICarrierAdapterFactory.cs`
- `Carrier/Interfaces/ICarrierSelector.cs`

**Acceptance:** `ICarrierAdapter` has exactly 3 methods + 1 property  
**Complexity:** Low  
**Dependencies:** Task 1.2, Task 1.3 (for `ServiceLevel`)

### Task 1.5 — Tracking Domain (Interfaces + Models + Enums)

**Files:**
- `Tracking/Enums/TrackingEventType.cs`
- `Tracking/Models/TrackingEvent.cs`
- `Tracking/Models/TrackingSnapshot.cs`
- `Tracking/Interfaces/ITrackingObserver.cs`
- `Tracking/Interfaces/ITrackingSubject.cs`
- `Tracking/Interfaces/ITrackingSource.cs`

**Acceptance:** `ITrackingSubject` has exactly 3 methods
**Complexity:** Low
**Dependencies:** Task 1.2, Task 1.4 (for `ShipmentStatus`)

### Task 1.6 — Orders Domain (Interfaces + Models + Enums)

**Files:**
- `Orders/Enums/OrderStatus.cs`
- `Orders/Enums/OrderAction.cs`
- `Orders/Models/Order.cs` (aggregate root class)
- `Orders/Models/OrderItem.cs`
- `Orders/Models/OrderDetail.cs`
- `Orders/Models/OrderSummary.cs`
- `Orders/Models/OrderFilter.cs`
- `Orders/Models/OrderDomainEvent.cs`
- `Orders/Interfaces/IOrderState.cs`
- `Orders/Interfaces/IOrderRepository.cs`
- `Orders/Interfaces/IOrderReadService.cs`

**Acceptance:** `Order.Apply()` delegates to `CurrentState.Transition()`
**Complexity:** High (aggregate root design is critical)
**Dependencies:** Task 1.2, Task 1.3 (for `ServiceLevel`)

### Task 1.7 — Audit Domain (Interfaces + Models + Enums)

**Files:**
- `Audit/Enums/AuditAction.cs`
- `Audit/Enums/AuditCategory.cs`
- `Audit/Models/AuditRecord.cs`
- `Audit/Models/AuditContext.cs`
- `Audit/Interfaces/IAuditSink.cs`
- `Audit/Interfaces/IAuditQueryService.cs`
- `Audit/Interfaces/IAuditInterceptor.cs`

**Acceptance:** `AuditRecord` is an immutable record
**Complexity:** Low
**Dependencies:** Task 1.1

**✅ Phase 1 Gate:** `dotnet build src/DtExpress.Domain/` passes with zero warnings. Domain `.csproj` has ZERO references.

---

## Phase 2: Application Layer

### Task 2.1 — CQRS Contracts

**Files:**
- `Common/ICommand.cs`
- `Common/IQuery.cs`
- `Common/ICommandHandler.cs`
- `Common/IQueryHandler.cs`
- `Common/ICommandDispatcher.cs`
- `Common/IQueryDispatcher.cs`
- `Common/IDomainEventPublisher.cs`

**Complexity:** Low
**Dependencies:** Task 1.6 (for `OrderDomainEvent`)

### Task 2.2 — Cross-Domain Ports

**Files:**
- `Ports/IRoutingPort.cs`
- `Ports/ICarrierPort.cs`
- `Ports/ITrackingPort.cs`
- `Ports/IAuditPort.cs`

**Complexity:** Low  
**Dependencies:** Tasks 1.3–1.7

### Task 2.3 — Application Services (Routing + Carrier)

**Files:**
- `Routing/RouteCalculationService.cs`
- `Routing/RouteComparisonService.cs`
- `Carrier/CarrierQuotingService.cs`
- `Carrier/CarrierBookingService.cs`

**Complexity:** Medium  
**Dependencies:** Tasks 2.1, 2.2

### Task 2.4 — Tracking Application Service

**Files:**
- `Tracking/TrackingSubscriptionService.cs`

**Complexity:** Low  
**Dependencies:** Task 2.2

### Task 2.5 — Order Commands + Queries + Handlers

**Files:**
- `Orders/Commands/CreateOrderCommand.cs`
- `Orders/Commands/ConfirmOrderCommand.cs`
- `Orders/Commands/ShipOrderCommand.cs`
- `Orders/Commands/DeliverOrderCommand.cs`
- `Orders/Commands/CancelOrderCommand.cs`
- `Orders/Queries/GetOrderByIdQuery.cs`
- `Orders/Queries/ListOrdersQuery.cs`
- `Orders/Handlers/CreateOrderHandler.cs`
- `Orders/Handlers/ConfirmOrderHandler.cs`
- `Orders/Handlers/ShipOrderHandler.cs`
- `Orders/Handlers/DeliverOrderHandler.cs`
- `Orders/Handlers/CancelOrderHandler.cs`
- `Orders/Handlers/GetOrderByIdHandler.cs`
- `Orders/Handlers/ListOrdersHandler.cs`

**Complexity:** High (most code in this task, orchestration logic)  
**Dependencies:** Tasks 2.1–2.4

**✅ Phase 2 Gate:** `dotnet build src/DtExpress.Application/` passes. Application `.csproj` references ONLY Domain.

---

## Phase 3: Infrastructure — Algorithms & Core

### Task 3.1 — Common Infrastructure

**Files:**
- `Common/SystemClock.cs`
- `Common/GuidIdGenerator.cs`
- `Common/CorrelationIdProvider.cs`
- `Common/CommandDispatcher.cs`
- `Common/QueryDispatcher.cs`
- `Common/InMemoryDomainEventPublisher.cs`

**Complexity:** Medium (CommandDispatcher uses reflection for generic dispatch)  
**Dependencies:** Phase 2

### Task 3.2 — Pathfinding Algorithms

**Files:**
- `Routing/Algorithms/AStarPathfinder.cs`
- `Routing/Algorithms/DijkstraPathfinder.cs`
- `Routing/Algorithms/WeightedScoreCalculator.cs`

**Acceptance:** A* and Dijkstra produce correct paths on test graph  
**Complexity:** High (core algorithm implementation)  
**Dependencies:** Task 1.3

### Task 3.3 — Mock Map Service

**Files:**
- `Routing/MockMapService.cs` — returns hardcoded graph (10–15 nodes representing CN cities)

**Acceptance:** Graph has at least 10 nodes, 15+ edges, realistic distances  
**Complexity:** Medium (data creation)  
**Dependencies:** Task 1.3

---

## Phase 4: Infrastructure — Patterns & Stores

### Task 4.1 — Route Strategies + Factory + Decorators

**Files:**
- `Routing/Strategies/FastestRouteStrategy.cs`
- `Routing/Strategies/CheapestRouteStrategy.cs`
- `Routing/Strategies/BalancedRouteStrategy.cs`
- `Routing/Decorators/CachingRouteDecorator.cs`
- `Routing/Decorators/LoggingRouteDecorator.cs`
- `Routing/Decorators/ValidationRouteDecorator.cs`
- `Routing/RouteStrategyFactory.cs`
- `Routing/Ports/RoutingPortAdapter.cs`

**Acceptance:** Factory uses dictionary registry, no switch/if-else  
**Complexity:** Medium  
**Dependencies:** Tasks 3.2, 3.3

### Task 4.2 — Carrier Adapters + Factory + Selectors

**Files:**
- `Carrier/Adapters/SfExpressAdapter.cs`
- `Carrier/Adapters/JdLogisticsAdapter.cs`
- `Carrier/MockData/SfMockResponses.cs`
- `Carrier/MockData/JdMockResponses.cs`
- `Carrier/Decorators/CachingCarrierDecorator.cs`
- `Carrier/Decorators/LoggingCarrierDecorator.cs`
- `Carrier/Selectors/CheapestCarrierSelector.cs`
- `Carrier/Selectors/FastestCarrierSelector.cs`
- `Carrier/CarrierAdapterFactory.cs`
- `Carrier/Ports/CarrierPortAdapter.cs`

**Acceptance:** SF adapter returns different data shape than JD; both map to same domain models
**Complexity:** Medium
**Dependencies:** Task 1.4

### Task 4.3 — Tracking Subject + Sources + Observers

**Files:**
- `Tracking/InMemoryTrackingSubject.cs`
- `Tracking/Sources/RandomTrackingSource.cs`
- `Tracking/Sources/ScriptedTrackingSource.cs`
- `Tracking/Observers/ConsoleTrackingObserver.cs`
- `Tracking/Ports/TrackingPortAdapter.cs`

**Acceptance:** Subscribe/publish/unsubscribe lifecycle works. Snapshot is updated on each event.
**Complexity:** Medium
**Dependencies:** Task 1.5

### Task 4.4 — Order States + In-Memory Stores

**Files:**
- `Orders/States/CreatedState.cs`
- `Orders/States/ConfirmedState.cs`
- `Orders/States/ShippedState.cs`
- `Orders/States/DeliveredState.cs`
- `Orders/States/CancelledState.cs`
- `Orders/InMemoryOrderRepository.cs`
- `Orders/InMemoryOrderReadService.cs`

**Acceptance:** State transition table matches spec exactly  
**Complexity:** Medium  
**Dependencies:** Task 1.6

### Task 4.5 — Audit Sink + PII Masking + Query

**Files:**
- `Audit/InMemoryAuditSink.cs`
- `Audit/InMemoryAuditQueryService.cs`
- `Audit/Decorators/PiiMaskingAuditDecorator.cs`
- `Audit/Interceptors/DomainEventAuditInterceptor.cs`
- `Orders/Ports/AuditPortAdapter.cs`

**Acceptance:** PII masking: `13812345678` → `138****5678`, `zhang@example.com` → `z***@example.com`
**Complexity:** Medium
**Dependencies:** Task 1.7

### Task 4.6 — DI Registration

**Files:**
- `DependencyInjection/ServiceCollectionExtensions.cs`
- `DependencyInjection/RoutingRegistration.cs`
- `DependencyInjection/CarrierRegistration.cs`
- `DependencyInjection/TrackingRegistration.cs`
- `DependencyInjection/OrderRegistration.cs`
- `DependencyInjection/AuditRegistration.cs`

**Acceptance:** `services.AddDtExpress()` wires everything. No switch/if-else anywhere.  
**Complexity:** Medium (must wire correctly)  
**Dependencies:** Tasks 4.1–4.5

**✅ Phase 4 Gate:** `dotnet build src/DtExpress.Infrastructure/` passes.

---

## Phase 5: API Layer

### Task 5.1 — Program.cs + Middleware

**Files:**
- `Program.cs`
- `Middleware/CorrelationIdMiddleware.cs`
- `Models/ApiResponse.cs`
- `appsettings.json`

**Acceptance:** `dotnet run` starts, Swagger loads at `/swagger`
**Complexity:** Low
**Dependencies:** Task 4.6

### Task 5.2 — Controllers

**Files:**
- `Controllers/RoutingController.cs`
- `Controllers/CarrierController.cs`
- `Controllers/TrackingController.cs`
- `Controllers/OrdersController.cs`
- `Controllers/AuditController.cs`

**Acceptance:** All 18 endpoints listed in API spec exist and return correct status codes  
**Complexity:** Medium  
**Dependencies:** Task 5.1

**✅ Phase 5 Gate:** `dotnet run --project src/DtExpress.Api/` starts. All endpoints visible in Swagger.

---

## Phase 6: Tests

### Task 6.1 — Domain Tests

**Files:** All files in `tests/DtExpress.Domain.Tests/` (~20 tests)  
**Complexity:** Medium  
**Dependencies:** Phase 1

### Task 6.2 — Application Tests (Fakes + Handler Tests)

**Files:** All files in `tests/DtExpress.Application.Tests/` (~12 tests)  
**Complexity:** High (need all fakes + handler assertions)  
**Dependencies:** Phase 2

### Task 6.3 — Infrastructure Tests

**Files:** All files in `tests/DtExpress.Infrastructure.Tests/` (~25 tests)  
**Complexity:** High (algorithm correctness, adapter mapping, decorator behavior)  
**Dependencies:** Phase 4

### Task 6.4 — API Integration Tests

**Files:** All files in `tests/DtExpress.Api.Tests/` (~10 tests)  
**Complexity:** Medium  
**Dependencies:** Phase 5

**✅ Phase 6 Gate:** `dotnet test` passes all ~70 tests.

---


**Files:**
- `README.md` — Quick start, architecture overview, pattern map, how to run

**Acceptance:** New developer can clone, build, test, and run in <5 minutes  
**Complexity:** Low  
**Dependencies:** All previous tasks

---

## Dependency Graph

```
Phase 0 ─► Phase 1 ─► Phase 2 ─► Phase 3 ─► Phase 4 ─► Phase 5
              │                      │           │           │
              │                      │           │           │
              ▼                      ▼           ▼           ▼
           Phase 6.1             Phase 6.2   Phase 6.3   Phase 6.4
              │                      │           │           │
              └──────────────────────┴───────────┴───────────┘
                                     │
                                     ▼
                                Phase 7
```

### Critical Path

```
Task 0.1 → 1.1 → 1.2 → 1.6 → 2.5 → 3.1 → 4.4 → 4.6 → 5.2 → 6.4
                                       ↑
                              (Order handlers depend on
                               all ports, routing, carrier)
```

### Parallelization Opportunities

| Can Run In Parallel | After |
|--------------------|-------|
| Tasks 1.3, 1.4, 1.5, 1.6, 1.7 | Task 1.2 |
| Tasks 2.3, 2.4 | Task 2.2 |
| Tasks 3.2, 3.3 | Task 1.3 |
| Tasks 4.1, 4.2, 4.3, 4.4, 4.5 | Phase 3 |
| Tasks 6.1, 6.2, 6.3, 6.4 | Their respective phases |

---

## Summary Statistics

| Metric | Value |
|--------|-------|
| Total phases | 6 |
| Total tasks | 26 |
| Total files (src/) | ~95 |
| Total files (tests/) | ~35 |
| Total interfaces | 30 |
| Total unit tests | ~70 |
| Total API endpoints | 18 |
| Estimated implementation time | 3–5 focused sessions |
