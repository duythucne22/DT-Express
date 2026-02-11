# 📜 02-INTERFACE-CONTRACTS — ISP-Verified Interface Catalog

> **Rule**: Every interface has ≤ 3 methods (properties don't count)  
> **Rule**: Domain interfaces have ZERO dependencies on other layers  
> **Rule**: Application interfaces reference only Domain types  
> **Verification**: Each interface below includes a method count and layer tag

---

## 📋 Table of Contents

1. [Domain Layer — Common](#domain-layer--common)
2. [Domain Layer — Routing (01)](#domain-layer--routing-01)
3. [Domain Layer — Carrier (02)](#domain-layer--carrier-02)
4. [Domain Layer — Tracking (03)](#domain-layer--tracking-03)
5. [Domain Layer — Orders (04)](#domain-layer--orders-04)
6. [Domain Layer — Audit (05)](#domain-layer--audit-05)
7. [Application Layer — CQRS](#application-layer--cqrs)
8. [Application Layer — Ports](#application-layer--ports)
9. [ISP Verification Matrix](#isp-verification-matrix)
10. [Dependency Direction Proof](#dependency-direction-proof)

---

## Domain Layer — Common

### `IClock`
```csharp
namespace DtExpress.Domain.Common;

/// <summary>Abstraction over system time for testability.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
```
> **Layer**: Domain · **Methods**: 0 · **Properties**: 1 · ✅ ISP

### `IIdGenerator`
```csharp
namespace DtExpress.Domain.Common;

/// <summary>Generates unique identifiers. Abstracted for deterministic testing.</summary>
public interface IIdGenerator
{
    Guid NewId();
}
```
> **Layer**: Domain · **Methods**: 1 · ✅ ISP

### `ICorrelationIdProvider`
```csharp
namespace DtExpress.Domain.Common;

/// <summary>Provides the current request's correlation ID for traceability.</summary>
public interface ICorrelationIdProvider
{
    string GetCorrelationId();
}
```
> **Layer**: Domain · **Methods**: 1 · ✅ ISP

---

## Domain Layer — Routing (01)

### `IRouteStrategy`
```csharp
namespace DtExpress.Domain.Routing.Interfaces;

/// <summary>
/// Strategy Pattern: interchangeable routing behavior.
/// Each implementation optimizes for a different metric (time, cost, balance).
/// Delegates computation to IPathfinder — does NOT contain algorithm math.
/// </summary>
public interface IRouteStrategy
{
    /// <summary>Human-readable name, e.g. "Fastest", "Cheapest", "Balanced".</summary>
    string Name { get; }

    /// <summary>
    /// Calculate a route for the given request.
    /// Business logic only — algorithm is delegated to IPathfinder.
    /// </summary>
    Route Calculate(RouteRequest request);
}
```
> **Layer**: Domain · **Methods**: 1 · **Properties**: 1 · ✅ ISP  
> **Pattern**: Strategy · **Implements**: `FastestRouteStrategy`, `CheapestRouteStrategy`, `BalancedRouteStrategy`

### `IPathfinder`
```csharp
namespace DtExpress.Domain.Routing.Interfaces;

/// <summary>
/// Algorithm abstraction: pure computation, zero business logic.
/// Separated from IRouteStrategy per SRP.
/// </summary>
public interface IPathfinder
{
    /// <summary>Find optimal path through graph from origin to destination node.</summary>
    PathResult FindPath(Graph graph, string fromNodeId, string toNodeId);
}
```
> **Layer**: Domain · **Methods**: 1 · ✅ ISP  
> **Implements**: `AStarPathfinder`, `DijkstraPathfinder`

### `IRouteStrategyFactory`
```csharp
namespace DtExpress.Domain.Routing.Interfaces;

/// <summary>
/// Factory Pattern: creates strategies by name from a registry.
/// No switch/if-else — uses dictionary populated by DI.
/// </summary>
public interface IRouteStrategyFactory
{
    /// <summary>Create (resolve) a strategy by its registered name.</summary>
    IRouteStrategy Create(string strategyName);

    /// <summary>List all available strategy names for comparison/discovery.</summary>
    IReadOnlyList<string> Available();
}
```
> **Layer**: Domain · **Methods**: 2 · ✅ ISP  
> **Pattern**: Factory

### `IMapService`
```csharp
namespace DtExpress.Domain.Routing.Interfaces;

/// <summary>
/// Infrastructure abstraction: provides graph data for pathfinding.
/// In production: calls map API. Here: returns hardcoded mock graph.
/// </summary>
public interface IMapService
{
    /// <summary>Build a navigation graph between two coordinates.</summary>
    Graph BuildGraph(GeoCoordinate origin, GeoCoordinate destination);
}
```
> **Layer**: Domain · **Methods**: 1 · ✅ ISP  
> **Implements**: `MockMapService`

---

## Domain Layer — Carrier (02)

### `ICarrierAdapter`
```csharp
namespace DtExpress.Domain.Carrier.Interfaces;

/// <summary>
/// Adapter Pattern: normalizes heterogeneous carrier APIs into unified operations.
/// Each implementation maps a specific carrier's data format (JSON/XML) to domain models.
/// </summary>
public interface ICarrierAdapter
{
    /// <summary>Carrier identifier, e.g. "SF", "JD".</summary>
    string CarrierCode { get; }

    /// <summary>Get a shipping quote from this carrier.</summary>
    Task<CarrierQuote> GetQuoteAsync(QuoteRequest request, CancellationToken ct = default);

    /// <summary>Book a shipment with this carrier.</summary>
    Task<BookingResult> BookAsync(BookingRequest request, CancellationToken ct = default);

    /// <summary>Get tracking info for a shipment.</summary>
    Task<CarrierTrackingInfo> TrackAsync(string trackingNumber, CancellationToken ct = default);
}
```
> **Layer**: Domain · **Methods**: 3 · **Properties**: 1 · ✅ ISP  
> **Pattern**: Adapter · **Implements**: `SfExpressAdapter`, `JdLogisticsAdapter`

### `ICarrierAdapterFactory`
```csharp
namespace DtExpress.Domain.Carrier.Interfaces;

/// <summary>
/// Factory Pattern: registry-based adapter resolution by carrier code.
/// No switch/if-else — dictionary built from DI IEnumerable injection.
/// </summary>
public interface ICarrierAdapterFactory
{
    /// <summary>Resolve adapter by carrier code (case-insensitive). Throws if not found.</summary>
    ICarrierAdapter Resolve(string carrierCode);

    /// <summary>Get all registered carrier adapters.</summary>
    IReadOnlyList<ICarrierAdapter> GetAll();
}
```
> **Layer**: Domain · **Methods**: 2 · ✅ ISP  
> **Pattern**: Factory

### `ICarrierSelector`
```csharp
namespace DtExpress.Domain.Carrier.Interfaces;

/// <summary>
/// Strategy Pattern: selects the best quote from a collection.
/// Different policies: cheapest, fastest, etc.
/// </summary>
public interface ICarrierSelector
{
    /// <summary>Select the best quote based on this selector's policy.</summary>
    CarrierQuote SelectBest(IEnumerable<CarrierQuote> quotes);
}
```
> **Layer**: Domain · **Methods**: 1 · ✅ ISP  
> **Pattern**: Strategy · **Implements**: `CheapestCarrierSelector`, `FastestCarrierSelector`

---

## Domain Layer — Tracking (03)

### `ITrackingObserver`
```csharp
namespace DtExpress.Domain.Tracking.Interfaces;

/// <summary>
/// Observer Pattern: receives tracking event notifications.
/// Implementations: console logger, dashboard, notification service, etc.
/// </summary>
public interface ITrackingObserver
{
    /// <summary>Called when a tracking event occurs for a subscribed tracking number.</summary>
    Task OnTrackingEventAsync(TrackingEvent evt, CancellationToken ct = default);
}
```
> **Layer**: Domain · **Methods**: 1 · ✅ ISP  
> **Pattern**: Observer · **Implements**: `ConsoleTrackingObserver`, `DashboardObserver`

### `ITrackingSubject`
```csharp
namespace DtExpress.Domain.Tracking.Interfaces;

/// <summary>
/// Observer Pattern: manages subscriptions and event distribution.
/// Subscribers receive only events for their tracking number (not a global broadcast).
/// </summary>
public interface ITrackingSubject
{
    /// <summary>Subscribe an observer to events for a specific tracking number. Returns disposable to unsubscribe.</summary>
    IDisposable Subscribe(string trackingNumber, ITrackingObserver observer);

    /// <summary>Publish an event. Notifies only observers subscribed to this event's tracking number.</summary>
    Task PublishAsync(TrackingEvent evt, CancellationToken ct = default);

    /// <summary>Get the latest known snapshot for a tracking number (for new subscribers).</summary>
    TrackingSnapshot? GetSnapshot(string trackingNumber);
}
```
> **Layer**: Domain · **Methods**: 3 · ✅ ISP  
> **Pattern**: Observer (Subject) · **Implements**: `InMemoryTrackingSubject`

### `ITrackingSource`
```csharp
namespace DtExpress.Domain.Tracking.Interfaces;

/// <summary>
/// Event source: produces tracking events (mock GPS, carrier webhooks, etc.).
/// Publishes into ITrackingSubject.
/// </summary>
public interface ITrackingSource
{
    /// <summary>Descriptive name of this source.</summary>
    string Name { get; }

    /// <summary>Start producing events. Runs until cancellation.</summary>
    Task StartAsync(CancellationToken ct = default);
}
```
> **Layer**: Domain · **Methods**: 1 · **Properties**: 1 · ✅ ISP  
> **Implements**: `RandomTrackingSource`, `ScriptedTrackingSource`

---

## Domain Layer — Orders (04)

### `IOrderState`
```csharp
namespace DtExpress.Domain.Orders.Interfaces;

/// <summary>
/// State Pattern: each concrete state defines valid transitions.
/// Context (Order) delegates all actions to current state.
/// Invalid transitions throw DomainException.
/// </summary>
public interface IOrderState
{
    /// <summary>The status this state represents.</summary>
    OrderStatus Status { get; }

    /// <summary>
    /// Attempt to transition the order based on an action.
    /// Returns the new state if valid, throws DomainException if invalid.
    /// </summary>
    IOrderState Transition(OrderAction action, Order context);

    /// <summary>Check if this state can handle the given action (for UI enablement).</summary>
    bool CanHandle(OrderAction action);
}
```
> **Layer**: Domain · **Methods**: 2 · **Properties**: 1 · ✅ ISP  
> **Pattern**: State · **Implements**: `CreatedState`, `ConfirmedState`, `ShippedState`, `DeliveredState`, `CancelledState`

### `IOrderRepository`
```csharp
namespace DtExpress.Domain.Orders.Interfaces;

/// <summary>Write-side persistence for the Order aggregate.</summary>
public interface IOrderRepository
{
    /// <summary>Load an order by ID. Returns null if not found.</summary>
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>Save (upsert) an order aggregate.</summary>
    Task SaveAsync(Order order, CancellationToken ct = default);
}
```
> **Layer**: Domain · **Methods**: 2 · ✅ ISP  
> **Implements**: `InMemoryOrderRepository`

### `IOrderReadService`
```csharp
namespace DtExpress.Domain.Orders.Interfaces;

/// <summary>Read-side queries for CQRS. Shaped for views, not aggregates.</summary>
public interface IOrderReadService
{
    /// <summary>Get full order detail for display.</summary>
    Task<OrderDetail?> GetByIdAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>List orders with optional filtering.</summary>
    Task<IReadOnlyList<OrderSummary>> ListAsync(OrderFilter filter, CancellationToken ct = default);
}
```
> **Layer**: Domain · **Methods**: 2 · ✅ ISP  
> **Implements**: `InMemoryOrderReadService`

---

## Domain Layer — Audit (05)

### `IAuditSink`
```csharp
namespace DtExpress.Domain.Audit.Interfaces;

/// <summary>
/// Append-only audit record storage.
/// Decorator target: PII masking wraps this interface.
/// </summary>
public interface IAuditSink
{
    /// <summary>Append a single audit record. Records are immutable once written.</summary>
    Task AppendAsync(AuditRecord record, CancellationToken ct = default);
}
```
> **Layer**: Domain · **Methods**: 1 · ✅ ISP  
> **Pattern**: Decorator target · **Implements**: `InMemoryAuditSink`, `PiiMaskingAuditDecorator`

### `IAuditQueryService`
```csharp
namespace DtExpress.Domain.Audit.Interfaces;

/// <summary>Read-only queries over the audit stream. Never mutates.</summary>
public interface IAuditQueryService
{
    /// <summary>Get audit trail for a specific entity (timeline view).</summary>
    Task<IReadOnlyList<AuditRecord>> GetByEntityAsync(
        string entityType, string entityId, CancellationToken ct = default);

    /// <summary>Get all audit records sharing a correlation ID (request tracing).</summary>
    Task<IReadOnlyList<AuditRecord>> GetByCorrelationAsync(
        string correlationId, CancellationToken ct = default);
}
```
> **Layer**: Domain · **Methods**: 2 · ✅ ISP  
> **Implements**: `InMemoryAuditQueryService`

### `IAuditInterceptor`
```csharp
namespace DtExpress.Domain.Audit.Interfaces;

/// <summary>
/// Interceptor Pattern: captures changes from a context and produces audit records.
/// Called at domain boundaries (state transitions, external calls).
/// </summary>
public interface IAuditInterceptor
{
    /// <summary>Capture changes from the given context, return audit records to append.</summary>
    IReadOnlyList<AuditRecord> CaptureChanges(AuditContext context);
}
```
> **Layer**: Domain · **Methods**: 1 · ✅ ISP
> **Pattern**: Interceptor · **Implements**: `DomainEventAuditInterceptor`

---

## Application Layer — CQRS

### `ICommand<TResult>` (marker)
```csharp
namespace DtExpress.Application.Common;

/// <summary>Marker interface for CQRS commands. TResult is the return type.</summary>
public interface ICommand<TResult> { }
```
> **Layer**: Application · **Methods**: 0 · ✅ ISP

### `IQuery<TResult>` (marker)
```csharp
namespace DtExpress.Application.Common;

/// <summary>Marker interface for CQRS queries. TResult is the return type.</summary>
public interface IQuery<TResult> { }
```
> **Layer**: Application · **Methods**: 0 · ✅ ISP

### `ICommandHandler<TCommand, TResult>`
```csharp
namespace DtExpress.Application.Common;

/// <summary>Handles a specific command type. One handler per command.</summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}
```
> **Layer**: Application · **Methods**: 1 · ✅ ISP

### `IQueryHandler<TQuery, TResult>`
```csharp
namespace DtExpress.Application.Common;

/// <summary>Handles a specific query type. One handler per query.</summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}
```
> **Layer**: Application · **Methods**: 1 · ✅ ISP

### `ICommandDispatcher`
```csharp
namespace DtExpress.Application.Common;

/// <summary>
/// Dispatches commands to their handlers via DI resolution.
/// No switch/if-else — uses IServiceProvider.GetRequiredService with generic type.
/// </summary>
public interface ICommandDispatcher
{
    Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default);
}
```
> **Layer**: Application · **Methods**: 1 · ✅ ISP

### `IQueryDispatcher`
```csharp
namespace DtExpress.Application.Common;

/// <summary>Dispatches queries to their handlers via DI resolution.</summary>
public interface IQueryDispatcher
{
    Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}
```
> **Layer**: Application · **Methods**: 1 · ✅ ISP

### `IDomainEventPublisher`
```csharp
namespace DtExpress.Application.Common;

/// <summary>Publishes domain events to interested listeners (audit, tracking, etc.).</summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(OrderDomainEvent domainEvent, CancellationToken ct = default);
}
```
> **Layer**: Application · **Methods**: 1 · ✅ ISP

---

## Application Layer — Ports

### `IRoutingPort`
```csharp
namespace DtExpress.Application.Ports;

/// <summary>Cross-domain port: Orders → Routing. Used by ShipOrderHandler.</summary>
public interface IRoutingPort
{
    Task<Route> CalculateRouteAsync(RouteRequest request, CancellationToken ct = default);
}
```
> **Layer**: Application · **Methods**: 1 · ✅ ISP

### `ICarrierPort`
```csharp
namespace DtExpress.Application.Ports;

/// <summary>Cross-domain port: Orders → Carrier. Used by ShipOrderHandler.</summary>
public interface ICarrierPort
{
    Task<IReadOnlyList<CarrierQuote>> GetQuotesAsync(QuoteRequest request, CancellationToken ct = default);
    Task<BookingResult> BookBestAsync(QuoteRequest request, CancellationToken ct = default);
}
```
> **Layer**: Application · **Methods**: 2 · ✅ ISP

### `ITrackingPort`
```csharp
namespace DtExpress.Application.Ports;

/// <summary>Cross-domain port: Orders → Tracking. Used by GetOrderByIdHandler.</summary>
public interface ITrackingPort
{
    Task<TrackingSnapshot?> GetSnapshotAsync(string trackingNumber, CancellationToken ct = default);
}
```
> **Layer**: Application · **Methods**: 1 · ✅ ISP

### `IAuditPort`
```csharp
namespace DtExpress.Application.Ports;

/// <summary>Cross-domain port: All handlers → Audit. Records state changes and actions.</summary>
public interface IAuditPort
{
    Task RecordAsync(AuditContext context, CancellationToken ct = default);
}
```
> **Layer**: Application · **Methods**: 1 · ✅ ISP

---

## ISP Verification Matrix

| Interface | Layer | Methods | Properties | Total Members | ≤3 Methods? |
|-----------|-------|---------|------------|---------------|-------------|
| `IClock` | Domain | 0 | 1 | 1 | ✅ |
| `IIdGenerator` | Domain | 1 | 0 | 1 | ✅ |
| `ICorrelationIdProvider` | Domain | 1 | 0 | 1 | ✅ |
| `IRouteStrategy` | Domain | 1 | 1 | 2 | ✅ |
| `IPathfinder` | Domain | 1 | 0 | 1 | ✅ |
| `IRouteStrategyFactory` | Domain | 2 | 0 | 2 | ✅ |
| `IMapService` | Domain | 1 | 0 | 1 | ✅ |
| `ICarrierAdapter` | Domain | 3 | 1 | 4 | ✅ |
| `ICarrierAdapterFactory` | Domain | 2 | 0 | 2 | ✅ |
| `ICarrierSelector` | Domain | 1 | 0 | 1 | ✅ |
| `ITrackingObserver` | Domain | 1 | 0 | 1 | ✅ |
| `ITrackingSubject` | Domain | 3 | 0 | 3 | ✅ |
| `ITrackingSource` | Domain | 1 | 1 | 2 | ✅ |
| `IOrderState` | Domain | 2 | 1 | 3 | ✅ |
| `IOrderRepository` | Domain | 2 | 0 | 2 | ✅ |
| `IOrderReadService` | Domain | 2 | 0 | 2 | ✅ |
| `IAuditSink` | Domain | 1 | 0 | 1 | ✅ |
| `IAuditQueryService` | Domain | 2 | 0 | 2 | ✅ |
| `IAuditInterceptor` | Domain | 1 | 0 | 1 | ✅ |
| `ICommand<T>` | Application | 0 | 0 | 0 | ✅ |
| `IQuery<T>` | Application | 0 | 0 | 0 | ✅ |
| `ICommandHandler<,>` | Application | 1 | 0 | 1 | ✅ |
| `IQueryHandler<,>` | Application | 1 | 0 | 1 | ✅ |
| `ICommandDispatcher` | Application | 1 | 0 | 1 | ✅ |
| `IQueryDispatcher` | Application | 1 | 0 | 1 | ✅ |
| `IDomainEventPublisher` | Application | 1 | 0 | 1 | ✅ |
| `IRoutingPort` | Application | 1 | 0 | 1 | ✅ |
| `ICarrierPort` | Application | 2 | 0 | 2 | ✅ |
| `ITrackingPort` | Application | 1 | 0 | 1 | ✅ |
| `IAuditPort` | Application | 1 | 0 | 1 | ✅ |

**Total interfaces**: 30  
**Maximum methods on any interface**: 3 (`ICarrierAdapter`, `ITrackingSubject`)  
**All pass ISP ≤3 check**: ✅

---

## Dependency Direction Proof

```
Domain interfaces depend on:
  ├── System.* types only (Guid, Task, string, IDisposable, etc.)
  ├── Domain value objects (Address, Money, Weight, etc.)
  ├── Domain models (RouteRequest, Route, Graph, Order, etc.)
  └── Domain enums (ServiceLevel, OrderStatus, etc.)

Application interfaces depend on:
  ├── Domain types (via project reference)
  └── Application types (ICommand<T>, IQuery<T>)

Infrastructure implements ALL interfaces.
Api consumes Application services + dispatchers.
```

> **No circular dependencies. No upward dependencies. All arrows point inward.** ✅
