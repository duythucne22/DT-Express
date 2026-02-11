# 📂 01-PROJECT-STRUCTURE — Complete File Tree

> **Purpose**: Every file in the solution, annotated with purpose and layer ownership  
> **Rule**: A worker agent should be able to create every file from this listing  
> **Verification**: After implementation, `tree /F` output matches this structure

---

## 📋 Table of Contents

1. [Solution Root](#solution-root)
2. [Domain Layer](#domain-layer-dtexpressdomain)
3. [Application Layer](#application-layer-dtexpressapplication)
4. [Infrastructure Layer](#infrastructure-layer-dtexpressinfrastructure)
5. [Api Layer](#api-layer-dtexpressapi)
6. [Test Projects](#test-projects)
7. [Project Reference Matrix](#project-reference-matrix)
8. [File Creation Order](#file-creation-order)

---

## Solution Root

```
DtExpress.sln
│
├── src/
│   ├── DtExpress.Domain/
│   ├── DtExpress.Application/
│   ├── DtExpress.Infrastructure/
│   └── DtExpress.Api/
│
├── tests/
│   ├── DtExpress.Domain.Tests/
│   ├── DtExpress.Application.Tests/
│   ├── DtExpress.Infrastructure.Tests/
│   └── DtExpress.Api.Tests/
│
├── docs/                               ← Existing documentation
    ├── architecture/
├── LICENSE
└── README.md
```

---

## Domain Layer (`DtExpress.Domain`)

> **Rule**: ZERO project references. ZERO NuGet packages. Pure C# only.  
> **Contains**: Interfaces, value objects, enums, domain models, domain exceptions.  
> **Does NOT contain**: Any implementation logic, any `using` of external namespaces.

```
src/DtExpress.Domain/
├── DtExpress.Domain.csproj             ← net8.0, NO references
│
├── Common/                              ← Cross-cutting domain abstractions
│   ├── IClock.cs                        ← { DateTimeOffset UtcNow { get; } }
│   ├── IIdGenerator.cs                  ← { Guid NewId(); }
│   ├── ICorrelationIdProvider.cs        ← { string GetCorrelationId(); }
│   └── DomainException.cs              ← Base exception for domain violations
│
├── ValueObjects/                        ← Immutable, equality-by-value
│   ├── Address.cs                       ← Street, City, Province, PostalCode, Country
│   ├── GeoCoordinate.cs                 ← Lat, Lng + DistanceTo()
│   ├── Money.cs                         ← Amount, Currency + arithmetic
│   ├── Weight.cs                        ← Value, Unit + ToKilograms()
│   ├── Dimension.cs                     ← Length, Width, Height, Unit
│   ├── ContactInfo.cs                   ← Name, Phone, Email
│   └── TrackingNumber.cs               ← Value wrapper with validation
│
├── Routing/                             ← Domain 01: Dynamic Routing
│   ├── Interfaces/
│   │   ├── IRouteStrategy.cs            ← { string Name; Route Calculate(RouteRequest); }
│   │   ├── IPathfinder.cs              ← { PathResult FindPath(Graph, string, string); }
│   │   ├── IRouteStrategyFactory.cs    ← { IRouteStrategy Create(string name); IReadOnlyList<string> Available(); }
│   │   └── IMapService.cs             ← { Graph BuildGraph(GeoCoordinate from, GeoCoordinate to); }
│   ├── Models/
│   │   ├── RouteRequest.cs             ← Origin, Destination, Package, ServiceLevel
│   │   ├── Route.cs                     ← Waypoints, Distance, Duration, Cost, StrategyUsed
│   │   ├── Graph.cs                     ← Nodes dictionary, Edges list
│   │   ├── GraphNode.cs                ← Id, Coordinate, Name
│   │   ├── GraphEdge.cs                ← FromId, ToId, Distance, Duration, Cost
│   │   └── PathResult.cs              ← NodeIds path, TotalDistance, TotalDuration, TotalCost
│   └── Enums/
│       ├── ServiceLevel.cs             ← Express, Standard, Economy
│       └── RouteOptimization.cs        ← Fastest, Cheapest, Balanced
│
├── Carrier/                             ← Domain 02: Multi-Carrier
│   ├── Interfaces/
│   │   ├── ICarrierAdapter.cs          ← { string CarrierCode; Task<Quote> GetQuoteAsync(...); Task<BookResult> BookAsync(...); Task<TrackInfo> TrackAsync(...); }
│   │   ├── ICarrierAdapterFactory.cs   ← { ICarrierAdapter Resolve(string code); IReadOnlyList<ICarrierAdapter> GetAll(); }
│   │   └── ICarrierSelector.cs         ← { CarrierQuote SelectBest(IEnumerable<CarrierQuote>); }
│   ├── Models/
│   │   ├── QuoteRequest.cs             ← Origin, Destination, Weight, ServiceLevel
│   │   ├── CarrierQuote.cs             ← CarrierCode, Price, EstimatedDays, ServiceLevel
│   │   ├── BookingRequest.cs           ← CarrierCode, Origin, Destination, Weight, ContactInfo
│   │   ├── BookingResult.cs            ← CarrierCode, TrackingNumber, BookedAt
│   │   └── CarrierTrackingInfo.cs      ← TrackingNumber, Status, Location, UpdatedAt
│   └── Enums/
│       ├── CarrierCode.cs              ← SF, JD (string constants, not enum)
│       └── ShipmentStatus.cs           ← Created, PickedUp, InTransit, OutForDelivery, Delivered, Exception
│
├── Tracking/                            ← Domain 03: Realtime Tracking
│   ├── Interfaces/
│   │   ├── ITrackingObserver.cs        ← { Task OnTrackingEventAsync(TrackingEvent evt); }
│   │   ├── ITrackingSubject.cs         ← { IDisposable Subscribe(string trackingNo, ITrackingObserver); Task PublishAsync(TrackingEvent); TrackingSnapshot? GetSnapshot(string trackingNo); }
│   │   └── ITrackingSource.cs          ← { string Name { get; } Task StartAsync(CancellationToken); }
│   ├── Models/
│   │   ├── TrackingEvent.cs            ← TrackingNumber, EventType, Location?, Status?, OccurredAt
│   │   └── TrackingSnapshot.cs         ← TrackingNumber, CurrentStatus, LastLocation, UpdatedAt
│   └── Enums/
│       └── TrackingEventType.cs        ← StatusChanged, LocationUpdated
│
├── Orders/                              ← Domain 04: Order Processing
│   ├── Interfaces/
│   │   ├── IOrderState.cs              ← { OrderStatus Status; IOrderState Transition(OrderAction, Order); bool CanHandle(OrderAction); }
│   │   ├── IOrderRepository.cs         ← { Task<Order?> GetByIdAsync(Guid); Task SaveAsync(Order); }
│   │   └── IOrderReadService.cs        ← { Task<OrderDetail?> GetByIdAsync(Guid); Task<IReadOnlyList<OrderSummary>> ListAsync(OrderFilter); }
│   ├── Models/
│   │   ├── Order.cs                     ← Aggregate root: Id, Items, State, Events, Customer info
│   │   ├── OrderItem.cs                ← Description, Quantity, Weight, Dimensions
│   │   ├── OrderDetail.cs              ← Read model: full order view for queries
│   │   ├── OrderSummary.cs             ← Read model: list view
│   │   ├── OrderFilter.cs              ← Status?, CustomerId?, DateRange
│   │   └── OrderDomainEvent.cs         ← Base class for order events
│   └── Enums/
│       ├── OrderStatus.cs              ← Created, Confirmed, Shipped, Delivered, Cancelled
│       └── OrderAction.cs              ← Confirm, Ship, Deliver, Cancel
│
└── Audit/                               ← Domain 05: Audit Tracking
    ├── Interfaces/
    │   ├── IAuditSink.cs               ← { Task AppendAsync(AuditRecord); }
    │   ├── IAuditQueryService.cs       ← { Task<IReadOnlyList<AuditRecord>> GetByEntityAsync(string, string); Task<IReadOnlyList<AuditRecord>> GetByCorrelationAsync(string); }
    │   └── IAuditInterceptor.cs        ← { IReadOnlyList<AuditRecord> CaptureChanges(AuditContext); }
    ├── Models/
    │   ├── AuditRecord.cs              ← Id, EntityType, EntityId, Action, Actor, Timestamp, CorrelationId, Payload
    │   └── AuditContext.cs             ← EntityType, EntityId, Action, Before, After, Actor
    └── Enums/
        ├── AuditAction.cs              ← Created, Updated, Deleted, StateChanged, BusinessAction
        └── AuditCategory.cs            ← DataChange, StateTransition, ExternalCall, BusinessDecision
```

---

## Application Layer (`DtExpress.Application`)

> **Rule**: References ONLY `DtExpress.Domain`. Contains orchestration logic.  
> **Contains**: Command/query contracts, handlers, application services, cross-domain ports.  
> **Does NOT contain**: Implementations of domain interfaces, HTTP concerns, persistence.

```
src/DtExpress.Application/
├── DtExpress.Application.csproj        ← refs: DtExpress.Domain only
│
├── Common/                              ← CQRS infrastructure contracts
│   ├── ICommand.cs                     ← ICommand<TResult> marker interface
│   ├── IQuery.cs                       ← IQuery<TResult> marker interface
│   ├── ICommandHandler.cs             ← { Task<TResult> HandleAsync(TCommand, CancellationToken); }
│   ├── IQueryHandler.cs               ← { Task<TResult> HandleAsync(TQuery, CancellationToken); }
│   ├── ICommandDispatcher.cs          ← { Task<TResult> DispatchAsync<TResult>(ICommand<TResult>, CancellationToken); }
│   ├── IQueryDispatcher.cs            ← { Task<TResult> DispatchAsync<TResult>(IQuery<TResult>, CancellationToken); }
│   └── IDomainEventPublisher.cs       ← { Task PublishAsync(OrderDomainEvent evt); }
│
├── Ports/                               ← Cross-domain boundary abstractions
│   ├── IRoutingPort.cs                 ← { Task<Route> CalculateRouteAsync(RouteRequest); }
│   ├── ICarrierPort.cs                ← { Task<IReadOnlyList<CarrierQuote>> GetQuotesAsync(QuoteRequest); Task<BookingResult> BookAsync(BookingRequest); }
│   ├── ITrackingPort.cs               ← { Task<TrackingSnapshot?> GetSnapshotAsync(string trackingNo); }
│   └── IAuditPort.cs                  ← { Task RecordAsync(AuditContext context); }
│
├── Routing/                             ← Domain 01 application services
│   ├── RouteCalculationService.cs      ← Orchestrates strategy selection + calculation
│   └── RouteComparisonService.cs       ← Runs all strategies, returns comparison
│
├── Carrier/                             ← Domain 02 application services
│   ├── CarrierQuotingService.cs        ← Gets quotes from all adapters, selects best
│   └── CarrierBookingService.cs        ← Books with selected carrier
│
├── Tracking/                            ← Domain 03 application services
│   └── TrackingSubscriptionService.cs  ← Manages observer subscriptions
│
└── Orders/                              ← Domain 04 CQRS commands + queries
    ├── Commands/
    │   ├── CreateOrderCommand.cs       ← { record: CustomerName, Items[], Origin, Dest, ServiceLevel } → Guid
    │   ├── ConfirmOrderCommand.cs      ← { record: OrderId } → bool
    │   ├── ShipOrderCommand.cs         ← { record: OrderId } → BookingResult
    │   ├── DeliverOrderCommand.cs      ← { record: OrderId } → bool
    │   └── CancelOrderCommand.cs       ← { record: OrderId, Reason } → bool
    ├── Queries/
    │   ├── GetOrderByIdQuery.cs        ← { record: OrderId } → OrderDetail?
    │   └── ListOrdersQuery.cs          ← { record: OrderFilter } → IReadOnlyList<OrderSummary>
    └── Handlers/
        ├── CreateOrderHandler.cs       ← Validates + creates Order + persists + publishes event
        ├── ConfirmOrderHandler.cs      ← Loads Order + transitions state + persists
        ├── ShipOrderHandler.cs         ← Routes + Books carrier + transitions state + persists
        ├── DeliverOrderHandler.cs      ← Transitions state + persists
        ├── CancelOrderHandler.cs       ← Guard: only from Created/Confirmed + transitions + persists
        ├── GetOrderByIdHandler.cs      ← Reads from IOrderReadService
        └── ListOrdersHandler.cs        ← Reads from IOrderReadService with filter
```

---

## Infrastructure Layer (`DtExpress.Infrastructure`)

> **Rule**: References `DtExpress.Application` + `DtExpress.Domain`.  
> **Contains**: All concrete implementations, mock services, decorators, DI wiring.  
> **This is where ALL the pattern implementation code lives.**

```
src/DtExpress.Infrastructure/
├── DtExpress.Infrastructure.csproj     ← refs: Application + Domain
│
├── Common/                              ← Cross-cutting implementations
│   ├── SystemClock.cs                  ← IClock → DateTimeOffset.UtcNow
│   ├── GuidIdGenerator.cs             ← IIdGenerator → Guid.NewGuid()
│   ├── CorrelationIdProvider.cs        ← ICorrelationIdProvider → AsyncLocal<string>
│   ├── CommandDispatcher.cs            ← ICommandDispatcher → resolves ICommandHandler<,> from DI
│   ├── QueryDispatcher.cs             ← IQueryDispatcher → resolves IQueryHandler<,> from DI
│   └── InMemoryDomainEventPublisher.cs ← IDomainEventPublisher → audit + observer bridge
│
├── Routing/                             ← Domain 01 implementations
│   ├── Strategies/
│   │   ├── FastestRouteStrategy.cs     ← IRouteStrategy: uses IPathfinder (A*), optimizes time
│   │   ├── CheapestRouteStrategy.cs    ← IRouteStrategy: uses IPathfinder (Dijkstra), optimizes cost
│   │   └── BalancedRouteStrategy.cs    ← IRouteStrategy: uses weighted scoring, balances time+cost
│   ├── Algorithms/
│   │   ├── AStarPathfinder.cs          ← IPathfinder: A* with heuristic
│   │   ├── DijkstraPathfinder.cs       ← IPathfinder: classic shortest-path
│   │   └── WeightedScoreCalculator.cs  ← Internal helper for balanced strategy
│   ├── Decorators/
│   │   ├── CachingRouteDecorator.cs    ← IRouteStrategy decorator: ConcurrentDictionary cache
│   │   ├── LoggingRouteDecorator.cs    ← IRouteStrategy decorator: ILogger before/after
│   │   └── ValidationRouteDecorator.cs ← IRouteStrategy decorator: validates request before delegating
│   ├── MockMapService.cs              ← IMapService: returns hardcoded graph (10-15 nodes)
│   ├── RouteStrategyFactory.cs         ← IRouteStrategyFactory: dictionary registry from DI
│   └── Ports/
│       └── RoutingPortAdapter.cs       ← IRoutingPort → uses IRouteStrategyFactory + IRouteStrategy
│
├── Carrier/                             ← Domain 02 implementations
│   ├── Adapters/
│   │   ├── SfExpressAdapter.cs         ← ICarrierAdapter: mock SF Express (JSON-style response)
│   │   └── JdLogisticsAdapter.cs       ← ICarrierAdapter: mock JD Logistics (XML-style response)
│   ├── MockData/
│   │   ├── SfMockResponses.cs          ← Static mock data for SF quotes/booking/tracking
│   │   └── JdMockResponses.cs          ← Static mock data for JD quotes/booking/tracking
│   ├── Decorators/
│   │   ├── CachingCarrierDecorator.cs  ← ICarrierAdapter decorator: caches quotes
│   │   └── LoggingCarrierDecorator.cs  ← ICarrierAdapter decorator: logs operations
│   ├── Selectors/
│   │   ├── CheapestCarrierSelector.cs  ← ICarrierSelector: picks lowest price
│   │   └── FastestCarrierSelector.cs   ← ICarrierSelector: picks fewest estimated days
│   ├── CarrierAdapterFactory.cs        ← ICarrierAdapterFactory: dictionary registry from DI
│   └── Ports/
│       └── CarrierPortAdapter.cs       ← ICarrierPort → uses factory + selector
│
├── Tracking/                            ← Domain 03 implementations
│   ├── InMemoryTrackingSubject.cs      ← ITrackingSubject: per-tracking-number observer registry
│   ├── Sources/
│   │   ├── RandomTrackingSource.cs     ← ITrackingSource: random events every N seconds
│   │   └── ScriptedTrackingSource.cs   ← ITrackingSource: deterministic event replay
│   ├── Observers/
│   │   └── ConsoleTrackingObserver.cs  ← ITrackingObserver: writes to ILogger (demo)
│   └── Ports/
│       └── TrackingPortAdapter.cs      ← ITrackingPort → reads snapshot from subject
│
├── Orders/                              ← Domain 04 implementations
│   ├── States/
│   │   ├── CreatedState.cs             ← IOrderState: allows Confirm, Cancel
│   │   ├── ConfirmedState.cs           ← IOrderState: allows Ship, Cancel
│   │   ├── ShippedState.cs             ← IOrderState: allows Deliver
│   │   ├── DeliveredState.cs           ← IOrderState: terminal, no transitions
│   │   └── CancelledState.cs           ← IOrderState: terminal, no transitions
│   ├── InMemoryOrderRepository.cs      ← IOrderRepository: ConcurrentDictionary store
│   ├── InMemoryOrderReadService.cs     ← IOrderReadService: reads from same dictionary (CQRS lite)
│   └── Ports/
│       └── AuditPortAdapter.cs         ← IAuditPort → bridges to IAuditSink
│
├── Audit/                               ← Domain 05 implementations
│   ├── InMemoryAuditSink.cs            ← IAuditSink: appends to List<AuditRecord>
│   ├── InMemoryAuditQueryService.cs    ← IAuditQueryService: LINQ queries over List
│   ├── Decorators/
│   │   └── PiiMaskingAuditDecorator.cs ← IAuditSink decorator: masks phone/email/address
│   └── Interceptors/
│       └── DomainEventAuditInterceptor.cs ← IAuditInterceptor: captures domain event → audit record
│
└── DependencyInjection/                 ← Composition root (DI wiring)
    ├── ServiceCollectionExtensions.cs   ← Single entry point: services.AddDtExpress()
    ├── RoutingRegistration.cs           ← Routing strategies + factory + decorators
    ├── CarrierRegistration.cs           ← Carrier adapters + factory + decorators
    ├── TrackingRegistration.cs          ← Subject + sources + observers
    ├── OrderRegistration.cs             ← States + repository + handlers
    └── AuditRegistration.cs             ← Sink + query + interceptors + masking
```

---

## Api Layer (`DtExpress.Api`)

> **Rule**: References `DtExpress.Infrastructure` (transitive to all layers).  
> **Contains**: Controllers, middleware, Program.cs (composition root).  
> **Does NOT contain**: Business logic, pattern implementations.

```
src/DtExpress.Api/
├── DtExpress.Api.csproj                ← refs: Infrastructure; PackageRef: Swashbuckle.AspNetCore
├── Program.cs                          ← Composition root: builder + services.AddDtExpress() + app pipeline
├── appsettings.json                    ← Minimal config (logging level)
│
├── Controllers/
│   ├── RoutingController.cs            ← [Route("api/routing")] — calculate, compare
│   ├── CarrierController.cs            ← [Route("api/carriers")] — list, quote, book, track
│   ├── TrackingController.cs           ← [Route("api/tracking")] — snapshot, subscribe (SSE)
│   ├── OrdersController.cs             ← [Route("api/orders")] — CRUD + state transitions
│   └── AuditController.cs             ← [Route("api/audit")] — timeline queries
│
├── Middleware/
│   └── CorrelationIdMiddleware.cs      ← Extracts/generates X-Correlation-Id header
│
└── Models/                              ← API-specific request/response DTOs (if needed)
    └── ApiResponse.cs                  ← Generic wrapper: { Success, Data, Error, CorrelationId }
```

---

## Test Projects

```
tests/
├── DtExpress.Domain.Tests/
│   ├── DtExpress.Domain.Tests.csproj   ← refs: Domain + xunit
│   ├── ValueObjects/
│   │   ├── MoneyTests.cs               ← Arithmetic, equality, currency validation
│   │   ├── WeightTests.cs              ← Conversion, comparison
│   │   ├── AddressTests.cs             ← Equality, immutability
│   │   └── GeoCoordinateTests.cs       ← Distance calculation, validation
│   ├── Routing/
│   │   └── GraphTests.cs              ← Graph construction, edge cases
│   └── Orders/
│       └── OrderTests.cs              ← State transitions, domain event emission
│
├── DtExpress.Application.Tests/
│   ├── DtExpress.Application.Tests.csproj ← refs: Application + Domain + xunit
│   └── Orders/
│       ├── CreateOrderHandlerTests.cs  ← Happy path, validation, idempotency
│       ├── ConfirmOrderHandlerTests.cs ← Valid/invalid state transitions
│       ├── ShipOrderHandlerTests.cs    ← Routing + booking orchestration
│       └── CancelOrderHandlerTests.cs  ← Guard conditions (only Created/Confirmed)
│
├── DtExpress.Infrastructure.Tests/
│   ├── DtExpress.Infrastructure.Tests.csproj ← refs: Infrastructure + Application + Domain + xunit
│   ├── Routing/
│   │   ├── AStarPathfinderTests.cs     ← Known graph → expected shortest path
│   │   ├── DijkstraPathfinderTests.cs  ← Known graph → expected cheapest path
│   │   ├── StrategyFactoryTests.cs     ← Registry resolution, unknown strategy
│   │   └── CachingDecoratorTests.cs    ← Cache hit/miss behavior
│   ├── Carrier/
│   │   ├── SfExpressAdapterTests.cs    ← Quote/book/track mapping correctness
│   │   ├── JdLogisticsAdapterTests.cs  ← Quote/book/track mapping correctness
│   │   ├── CheapestSelectorTests.cs    ← Selects minimum price
│   │   └── FactoryResolutionTests.cs   ← Code resolution, case-insensitive
│   ├── Tracking/
│   │   ├── TrackingSubjectTests.cs     ← Subscribe/publish/unsubscribe
│   │   └── SnapshotProjectionTests.cs  ← Latest state after multiple events
│   ├── Orders/
│   │   ├── StateTransitionTests.cs     ← All valid transitions + invalid → exception
│   │   ├── InMemoryRepoTests.cs        ← Save/load round-trip
│   │   └── ReadServiceTests.cs         ← Filter, list, detail
│   └── Audit/
│       ├── AuditSinkTests.cs           ← Append + query round-trip
│       └── PiiMaskingTests.cs          ← Phone/email/address masking rules
│
└── DtExpress.Api.Tests/
    ├── DtExpress.Api.Tests.csproj      ← refs: Api + Microsoft.AspNetCore.Mvc.Testing + xunit
    ├── RoutingControllerTests.cs       ← HTTP 200/400 for calculate/compare
    ├── CarrierControllerTests.cs       ← HTTP 200/400/404 for quote/book/track
    ├── OrdersControllerTests.cs        ← Full lifecycle via HTTP
    ├── AuditControllerTests.cs         ← Timeline query via HTTP
    └── SwaggerTests.cs                ← /swagger endpoint returns 200
```

---

## Project Reference Matrix

| Project | References | NuGet Packages |
|---------|-----------|----------------|
| `DtExpress.Domain` | **NONE** | **NONE** |
| `DtExpress.Application` | Domain | **NONE** |
| `DtExpress.Infrastructure` | Application, Domain | **NONE** |
| `DtExpress.Api` | Infrastructure | `Swashbuckle.AspNetCore` |
| `DtExpress.Domain.Tests` | Domain | `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` |
| `DtExpress.Application.Tests` | Application, Domain | Same test packages |
| `DtExpress.Infrastructure.Tests` | Infrastructure, Application, Domain | Same test packages |
| `DtExpress.Api.Tests` | Api | Same test packages + `Microsoft.AspNetCore.Mvc.Testing` |

### `.csproj` Dependency Verification Commands

```bash
# Verify Domain has zero references
findstr "ProjectReference\|PackageReference" src\DtExpress.Domain\DtExpress.Domain.csproj
# Expected: NO output

# Verify Application only refs Domain
findstr "ProjectReference" src\DtExpress.Application\DtExpress.Application.csproj
# Expected: DtExpress.Domain only

# Verify Infrastructure refs Application + Domain
findstr "ProjectReference" src\DtExpress.Infrastructure\DtExpress.Infrastructure.csproj
# Expected: DtExpress.Application + DtExpress.Domain
```

---

## File Creation Order

> For a worker agent: create files in this order to avoid compile errors.

### Phase 1: Domain Layer (all files compile independently)
1. Enums (all `Enums/` folders)
2. Value objects (all `ValueObjects/`)
3. Domain models (all `Models/`)
4. Interfaces (all `Interfaces/`)
5. `DomainException.cs`

### Phase 2: Application Layer
1. CQRS contracts (`Common/`)
2. Port interfaces (`Ports/`)
3. Commands + Queries (records)
4. Application services
5. Command/Query handlers

### Phase 3: Infrastructure Layer
1. Common implementations
2. Algorithm implementations (pathfinders)
3. Strategy implementations
4. Adapter implementations
5. Decorator implementations
6. State implementations
7. In-memory stores
8. Port adapters
9. Factory implementations
10. DI registration

### Phase 4: Api Layer
1. `Program.cs` (minimal)
2. Middleware
3. Controllers
4. `ApiResponse` wrapper

### Phase 5: Tests
1. Domain tests (value objects first)
2. Infrastructure tests (algorithms first)
3. Application tests (handlers)
4. Api tests (integration)
