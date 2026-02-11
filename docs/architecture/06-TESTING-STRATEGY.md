# 🧪 06-TESTING-STRATEGY — Test Plan, Mocking, Conventions

> **Framework**: xUnit (standard .NET test framework)
> **Mocking**: Manual mocks only (no Moq/NSubstitute — consistent with zero-external-deps rule)
> **Coverage Target**: Every public class has at least one test
> **Naming**: `MethodName_Scenario_ExpectedResult`

---

## 📋 Table of Contents

1. [Testing Philosophy](#testing-philosophy)
2. [Test Project Structure](#test-project-structure)
3. [Naming Conventions](#naming-conventions)
4. [Mocking Strategy](#mocking-strategy)
5. [Domain Layer Tests](#domain-layer-tests)
6. [Application Layer Tests](#application-layer-tests)
7. [Infrastructure Layer Tests](#infrastructure-layer-tests)
8. [API Layer Tests](#api-layer-tests)
9. [Test Data Builders](#test-data-builders)
10. [Acceptance Criteria Traceability](#acceptance-criteria-traceability)

---

## Testing Philosophy

### Testing Pyramid

```
        ┌───────────────┐
        │  API Tests    │  ~10 tests (integration, WebApplicationFactory)
        │  (broad, slow)│
        ├───────────────┤
        │  Application  │  ~15 tests (handler orchestration, mocked deps)
        │  Tests        │
        ├───────────────┤
        │ Infrastructure│  ~25 tests (algorithms, adapters, stores, decorators)
        │  Tests        │
        ├───────────────┤
        │  Domain Tests │  ~20 tests (value objects, models, state machine)
        │ (narrow, fast)│
        └───────────────┘
```

### Principles

| Principle | Application |
|-----------|-------------|
| **Test behavior, not implementation** | Assert outcomes, not internal method calls |
| **No external mocking library** | Hand-written stubs/fakes (implements interface) |
| **Each test is independent** | No shared mutable state between tests |
| **Arrange-Act-Assert** | Every test follows AAA structure |
| **Tests are documentation** | Test names describe business requirements |

---

## Test Project Structure

```
tests/
├── DtExpress.Domain.Tests/              ← Pure domain logic tests
│   ├── ValueObjects/
│   │   ├── MoneyTests.cs
│   │   ├── WeightTests.cs
│   │   ├── AddressTests.cs
│   │   ├── GeoCoordinateTests.cs
│   │   └── TrackingNumberTests.cs
│   ├── Routing/
│   │   └── GraphTests.cs
│   └── Orders/
│       ├── OrderStateTransitionTests.cs
│       └── OrderDomainEventTests.cs
│
├── DtExpress.Application.Tests/          ← Handler orchestration tests
│   ├── Fakes/                            ← Hand-written fakes for all interfaces
│   │   ├── FakeOrderRepository.cs
│   │   ├── FakeOrderReadService.cs
│   │   ├── FakeRoutingPort.cs
│   │   ├── FakeCarrierPort.cs
│   │   ├── FakeAuditPort.cs
│   │   ├── FakeClock.cs
│   │   ├── FakeIdGenerator.cs
│   │   └── FakeDomainEventPublisher.cs
│   └── Orders/
│       ├── CreateOrderHandlerTests.cs
│       ├── ConfirmOrderHandlerTests.cs
│       ├── ShipOrderHandlerTests.cs
│       ├── CancelOrderHandlerTests.cs
│       └── GetOrderByIdHandlerTests.cs
│
├── DtExpress.Infrastructure.Tests/       ← Implementation tests
│   ├── Routing/
│   │   ├── AStarPathfinderTests.cs
│   │   ├── DijkstraPathfinderTests.cs
│   │   ├── StrategyFactoryTests.cs
│   │   ├── CachingDecoratorTests.cs
│   │   └── MockMapServiceTests.cs
│   ├── Carrier/
│   │   ├── SfExpressAdapterTests.cs
│   │   ├── JdLogisticsAdapterTests.cs
│   │   ├── CarrierFactoryTests.cs
│   │   └── CheapestSelectorTests.cs
│   ├── Tracking/
│   │   ├── InMemoryTrackingSubjectTests.cs
│   │   └── SnapshotProjectionTests.cs
│   ├── Orders/
│   │   ├── InMemoryOrderRepoTests.cs
│   │   └── InMemoryReadServiceTests.cs
│   └── Audit/
│       ├── InMemoryAuditSinkTests.cs
│       └── PiiMaskingDecoratorTests.cs
│
└── DtExpress.Api.Tests/                  ← Integration tests
    ├── Infrastructure/
    │   └── DtExpressWebApplicationFactory.cs
    ├── RoutingControllerTests.cs
    ├── CarrierControllerTests.cs
    ├── OrdersControllerTests.cs
    ├── AuditControllerTests.cs
    └── SwaggerTests.cs
```

---

## Naming Conventions

### Test Method Names

**Pattern**: `MethodName_Scenario_ExpectedResult`

```csharp
// Value object tests
public void Add_SameCurrency_ReturnsSumAmount()
public void Add_DifferentCurrency_ThrowsInvalidOperation()
public void Constructor_NegativeAmount_ThrowsArgumentException()

// State machine tests
public void Transition_ConfirmFromCreated_ReturnsConfirmedState()
public void Transition_ShipFromCreated_ThrowsInvalidStateTransition()
public void CanHandle_CancelFromConfirmed_ReturnsTrue()
public void CanHandle_CancelFromDelivered_ReturnsFalse()

// Handler tests
public async Task HandleAsync_ValidCommand_CreatesOrderInCreatedState()
public async Task HandleAsync_InvalidOrigin_ThrowsDomainException()

// Infrastructure tests
public void FindPath_SimpleGraph_ReturnsShortestPath()
public async Task GetQuoteAsync_SfExpress_ReturnsMockQuote()
public void Resolve_UnknownCarrier_ThrowsCarrierNotFound()

// API tests
public async Task PostCreate_ValidRequest_Returns201WithOrderId()
public async Task PutConfirm_OrderNotFound_Returns404()
public async Task GetSwagger_Returns200()
```

### Test Class Names

```csharp
// Pattern: {ClassUnderTest}Tests
public class MoneyTests { }
public class CreatedStateTests { }
public class CreateOrderHandlerTests { }
public class SfExpressAdapterTests { }
public class RoutingControllerTests { }
```

---

## Mocking Strategy

### Why No Moq/NSubstitute

> The project constraint is zero external NuGet packages in `src/`.  
> For consistency, we also avoid mocking libraries in tests.  
> Hand-written fakes are more explicit and serve as documentation.

### Fake Pattern

```csharp
// tests/DtExpress.Application.Tests/Fakes/FakeOrderRepository.cs

public sealed class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _store = new();

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default)
        => Task.FromResult(_store.GetValueOrDefault(orderId));

    public Task SaveAsync(Order order, CancellationToken ct = default)
    {
        _store[order.Id] = order;
        return Task.CompletedTask;
    }

    // Test helpers (not part of interface)
    public int SavedCount => _store.Count;
    public Order? LastSaved => _store.Values.LastOrDefault();
}
```

### Standard Fakes Catalog

| Fake | Interface | Behavior |
|------|-----------|----------|
| `FakeOrderRepository` | `IOrderRepository` | In-memory dictionary |
| `FakeOrderReadService` | `IOrderReadService` | Returns pre-configured DTOs |
| `FakeRoutingPort` | `IRoutingPort` | Returns fixed Route |
| `FakeCarrierPort` | `ICarrierPort` | Returns fixed quotes + booking result |
| `FakeAuditPort` | `IAuditPort` | Records calls, asserts on captured records |
| `FakeClock` | `IClock` | Returns configurable fixed time |
| `FakeIdGenerator` | `IIdGenerator` | Returns sequential or fixed GUIDs |
| `FakeDomainEventPublisher` | `IDomainEventPublisher` | Captures published events for assertion |

### Fake Clock Example

```csharp
public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 2, 8, 10, 0, 0, TimeSpan.Zero);
    
    public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
}
```

---

## Domain Layer Tests

### Value Object Tests (~12 tests)

| Test Class | Test | Verifies |
|-----------|------|----------|
| `MoneyTests` | `Add_SameCurrency_ReturnsSumAmount` | Arithmetic correctness |
| `MoneyTests` | `Add_DifferentCurrency_ThrowsInvalidOperation` | Currency safety |
| `MoneyTests` | `Constructor_NegativeAmount_ThrowsArgument` | Invariant enforcement |
| `MoneyTests` | `Equals_SameAmountAndCurrency_ReturnsTrue` | Value equality |
| `WeightTests` | `ToKilograms_FromGrams_ConvertsCorrectly` | Unit conversion |
| `WeightTests` | `Constructor_ZeroWeight_ThrowsArgument` | Positive-only invariant |
| `AddressTests` | `Constructor_EmptyCity_ThrowsArgument` | Required field enforcement |
| `AddressTests` | `Equals_SameFields_ReturnsTrue` | Record equality |
| `GeoCoordinateTests` | `DistanceToKm_KnownPoints_ReturnsApproximateDistance` | Distance calc |
| `GeoCoordinateTests` | `Constructor_InvalidLatitude_ThrowsOutOfRange` | Range validation |
| `TrackingNumberTests` | `Constructor_TooShort_ThrowsArgument` | Min length enforcement |
| `TrackingNumberTests` | `ImplicitStringConversion_ReturnsValue` | Convenience operator |

### Order State Machine Tests (~8 tests)

| Test | From State | Action | Expected |
|------|-----------|--------|----------|
| `Transition_ConfirmFromCreated_ReturnsConfirmed` | Created | Confirm | ConfirmedState |
| `Transition_CancelFromCreated_ReturnsCancelled` | Created | Cancel | CancelledState |
| `Transition_ShipFromConfirmed_ReturnsShipped` | Confirmed | Ship | ShippedState |
| `Transition_CancelFromConfirmed_ReturnsCancelled` | Confirmed | Cancel | CancelledState |
| `Transition_DeliverFromShipped_ReturnsDelivered` | Shipped | Deliver | DeliveredState |
| `Transition_ConfirmFromShipped_ThrowsInvalid` | Shipped | Confirm | Exception |
| `Transition_AnyFromDelivered_ThrowsInvalid` | Delivered | Any | Exception |
| `Transition_AnyFromCancelled_ThrowsInvalid` | Cancelled | Any | Exception |

### Graph Tests (~2 tests)

| Test | Verifies |
|------|----------|
| `GetEdgesFrom_ExistingNode_ReturnsOutgoingEdges` | Edge retrieval |
| `GetEdgesFrom_UnknownNode_ReturnsEmpty` | No-crash on missing node |

---

## Application Layer Tests

### Handler Tests (~12 tests)

| Test Class | Test | Scenario |
|-----------|------|----------|
| `CreateOrderHandlerTests` | `HandleAsync_ValidCommand_SavesOrderInCreatedState` | Happy path |
| `CreateOrderHandlerTests` | `HandleAsync_ValidCommand_PublishesDomainEvent` | Event emission |
| `CreateOrderHandlerTests` | `HandleAsync_ValidCommand_RecordsAudit` | Cross-cutting audit |
| `ConfirmOrderHandlerTests` | `HandleAsync_CreatedOrder_TransitionsToConfirmed` | Valid transition |
| `ConfirmOrderHandlerTests` | `HandleAsync_DeliveredOrder_ThrowsDomainException` | Guard |
| `ShipOrderHandlerTests` | `HandleAsync_ConfirmedOrder_RoutesAndBooks` | Orchestration |
| `ShipOrderHandlerTests` | `HandleAsync_ConfirmedOrder_SetsTrackingInfo` | Carrier integration |
| `CancelOrderHandlerTests` | `HandleAsync_CreatedOrder_TransitionsToCancelled` | Cancel from Created |
| `CancelOrderHandlerTests` | `HandleAsync_ShippedOrder_ThrowsDomainException` | Guard |
| `GetOrderByIdHandlerTests` | `HandleAsync_ExistingOrder_ReturnsOrderDetail` | Happy path |
| `GetOrderByIdHandlerTests` | `HandleAsync_NonExistentOrder_ReturnsNull` | Not found |
| `ListOrdersHandlerTests` | `HandleAsync_WithStatusFilter_ReturnsFilteredList` | Query filtering |

---

## Infrastructure Layer Tests

### Algorithm Tests (~6 tests)

| Test Class | Test | Verifies |
|-----------|------|----------|
| `AStarPathfinderTests` | `FindPath_SimpleGraph_ReturnsShortestByTime` | A* correctness |
| `AStarPathfinderTests` | `FindPath_NoPath_ReturnsEmpty` | Unreachable handling |
| `AStarPathfinderTests` | `FindPath_DirectEdge_ReturnsSingleHop` | Base case |
| `DijkstraPathfinderTests` | `FindPath_SimpleGraph_ReturnsShortestByCost` | Dijkstra correctness |
| `DijkstraPathfinderTests` | `FindPath_NoPath_ReturnsEmpty` | Unreachable handling |
| `DijkstraPathfinderTests` | `FindPath_MultiplePathsSameCost_ReturnsOne` | Tie-breaking |

### Strategy Factory Tests (~3 tests)

| Test | Verifies |
|------|----------|
| `Create_KnownStrategy_ReturnsInstance` | Registry lookup works |
| `Create_UnknownStrategy_ThrowsNotFoundException` | Error handling |
| `Available_ReturnsAllRegisteredNames` | Discovery |

### Carrier Tests (~6 tests)

| Test | Verifies |
|------|----------|
| `SfExpress_GetQuoteAsync_ReturnsSfQuote` | Mock data mapping |
| `JdLogistics_GetQuoteAsync_ReturnsJdQuote` | Mock data mapping |
| `SfExpress_BookAsync_ReturnsTrackingNumber` | Booking mock |
| `JdLogistics_TrackAsync_ReturnsStatus` | Tracking mock |
| `CheapestSelector_MultipleQuotes_ReturnsLowestPrice` | Selection logic |
| `Factory_CaseInsensitive_ResolvesCorrectAdapter` | Case handling |

### Decorator Tests (~4 tests)

| Test | Verifies |
|------|----------|
| `CachingRoute_SecondCall_ReturnsCachedResult` | Cache hit |
| `CachingRoute_DifferentRequest_CallsInner` | Cache miss |
| `PiiMasking_PhoneNumber_MasksMiddleDigits` | `138****5678` |
| `PiiMasking_Email_MaskesUsername` | `z***@example.com` |

### Tracking Tests (~4 tests)

| Test | Verifies |
|------|----------|
| `Subscribe_Publish_ObserverReceivesEvent` | Basic pub/sub |
| `Subscribe_DifferentTrackingNo_ObserverDoesNotReceive` | Isolation |
| `Unsubscribe_Publish_ObserverDoesNotReceive` | Cleanup |
| `GetSnapshot_AfterPublish_ReturnsLatestState` | Projection |

### Store Tests (~4 tests)

| Test | Verifies |
|------|----------|
| `OrderRepo_SaveAndLoad_RoundTrips` | Persistence |
| `OrderRepo_GetNonExistent_ReturnsNull` | Missing key |
| `AuditSink_AppendAndQuery_RoundTrips` | Audit persistence |
| `AuditSink_QueryByCorrelation_FiltersCorrectly` | Query filtering |

---

## API Layer Tests

### Integration Test Setup

```csharp
// tests/DtExpress.Api.Tests/Infrastructure/DtExpressWebApplicationFactory.cs

public class DtExpressWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Uses the same AddDtExpress() registration — tests against real in-memory services
    }
}
```

### Controller Tests (~10 tests)

| Test Class | Test | Verifies |
|-----------|------|----------|
| `SwaggerTests` | `GetSwagger_Returns200` | Swagger endpoint works |
| `RoutingControllerTests` | `PostCalculate_ValidRequest_Returns200WithRoute` | Route calculation |
| `RoutingControllerTests` | `PostCompare_ValidRequest_Returns200WithAllStrategies` | Comparison |
| `RoutingControllerTests` | `GetStrategies_Returns200WithList` | Strategy discovery |
| `CarrierControllerTests` | `GetCarriers_Returns200WithList` | Carrier listing |
| `CarrierControllerTests` | `PostQuotes_ValidRequest_ReturnsQuotesFromAllCarriers` | Multi-carrier quotes |
| `OrdersControllerTests` | `PostCreate_ValidRequest_Returns201` | Order creation |
| `OrdersControllerTests` | `FullLifecycle_Create_Confirm_Ship_Deliver_AllReturn200` | End-to-end |
| `OrdersControllerTests` | `PutCancel_ShippedOrder_Returns400` | Invalid transition via HTTP |
| `AuditControllerTests` | `GetByEntity_AfterOrderCreation_ReturnsAuditTrail` | Audit integration |

---

## Test Data Builders

### Builder Pattern for Test Data

```csharp
// tests/DtExpress.Application.Tests/Fakes/TestOrderBuilder.cs

public sealed class TestOrderBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _orderNumber = "DT-TEST-001";
    private ContactInfo _customer = new("Test User", "13800000000");
    private Address _origin = new("Street A", "上海", "上海", "200000", "CN");
    private Address _destination = new("Street B", "北京", "北京", "100000", "CN");
    private List<OrderItem> _items = new() { new("Test Item", 1, Weight.Kilograms(1), null) };
    private ServiceLevel _serviceLevel = ServiceLevel.Standard;
    private IOrderState _state = new CreatedState();
    private DateTimeOffset _createdAt = new(2026, 2, 8, 10, 0, 0, TimeSpan.Zero);

    public TestOrderBuilder WithId(Guid id) { _id = id; return this; }
    public TestOrderBuilder WithStatus(IOrderState state) { _state = state; return this; }
    public TestOrderBuilder WithServiceLevel(ServiceLevel sl) { _serviceLevel = sl; return this; }

    public Order Build() => new(_id, _orderNumber, _customer, _origin, _destination,
        _items.AsReadOnly(), _serviceLevel, _state, _createdAt);
}
```

### Usage in Tests

```csharp
[Fact]
public async Task HandleAsync_ConfirmedOrder_TransitionsToShipped()
{
    // Arrange
    var order = new TestOrderBuilder()
        .WithStatus(new ConfirmedState())
        .Build();
    var repo = new FakeOrderRepository();
    await repo.SaveAsync(order);

    var handler = new ShipOrderHandler(repo, _fakeRoutingPort, _fakeCarrierPort, _fakeAuditPort, _fakeClock);

    // Act
    var result = await handler.HandleAsync(new ShipOrderCommand(order.Id));

    // Assert
    Assert.Equal(OrderStatus.Shipped, order.Status);
    Assert.NotNull(result.TrackingNumber);
}
```

---

## Acceptance Criteria Traceability

| Acceptance Criteria | Test That Verifies It |
|-------------------|-----------------------|
| Each strategy delegates to IPathfinder | `FastestRouteStrategy` uses `AStarPathfinder` (infra test) |
| Algorithms have zero business rules | Pathfinder tests use raw graph data, no domain models |
| Decorator cache hit skips inner strategy | `CachingRoute_SecondCall_ReturnsCachedResult` |
| Factory resolves by name, no switch | `StrategyFactory_Create_KnownStrategy_ReturnsInstance` |
| Adapter maps carrier format to domain | `SfExpress_GetQuoteAsync_ReturnsSfQuote` |
| Observer notifies only matching tracking# | `Subscribe_DifferentTrackingNo_ObserverDoesNotReceive` |
| State machine rejects invalid transitions | `Transition_ConfirmFromShipped_ThrowsInvalid` |
| CQRS commands mutate, queries don't | Handler tests verify writes; query tests verify reads only |
| PII masking decorator masks phone/email | `PiiMasking_PhoneNumber_MasksMiddleDigits` |
| Domain project has zero dependencies | CI check: no `<ProjectReference>` in Domain `.csproj` |
| All interfaces have ≤3 methods | Static analysis / code review |
| Swagger UI loads | `SwaggerTests.GetSwagger_Returns200` |

---

## Test Execution

```bash
# Run all tests
dotnet test

# Run specific layer
dotnet test tests/DtExpress.Domain.Tests/
dotnet test tests/DtExpress.Infrastructure.Tests/

# Run with verbosity
dotnet test --verbosity normal

# Run with filter
dotnet test --filter "FullyQualifiedName~OrderStateTransition"
```
