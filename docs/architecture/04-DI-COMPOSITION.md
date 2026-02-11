# 🔌 04-DI-COMPOSITION — Dependency Injection Wiring & OCP Proof

> **Rule**: Open/Closed Principle demonstrated via DI registration ONLY  
> **Rule**: No switch/if-else chains for pattern selection  
> **Rule**: Adding a new strategy/adapter/state = new class + one DI line  
> **Where**: `DtExpress.Infrastructure/DependencyInjection/`

---

## 📋 Table of Contents

1. [Composition Root](#composition-root)
2. [Routing Registration](#routing-registration)
3. [Carrier Registration](#carrier-registration)
4. [Tracking Registration](#tracking-registration)
5. [Order Registration](#order-registration)
6. [Audit Registration](#audit-registration)
7. [Common Registration](#common-registration)
8. [CQRS Dispatcher Wiring](#cqrs-dispatcher-wiring)
9. [OCP Proof: Adding New Implementations](#ocp-proof-adding-new-implementations)
10. [Anti-Patterns Avoided](#anti-patterns-avoided)

---

## Composition Root

### `Program.cs` (Api Layer)

```csharp
// src/DtExpress.Api/Program.cs

var builder = WebApplication.CreateBuilder(args);

// === Service Registration (single entry point) ===
builder.Services.AddDtExpress();  // <-- All 5 domains wired here

// === API Setup ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "DT-Express TMS API", Version = "v1" });
    options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
});

var app = builder.Build();

// === Middleware Pipeline ===
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
```

### `ServiceCollectionExtensions.cs` (Master Entry Point)

```csharp
// src/DtExpress.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs

namespace DtExpress.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all DT-Express services.
    /// Single entry point — all 5 domains + cross-cutting concerns.
    /// </summary>
    public static IServiceCollection AddDtExpress(this IServiceCollection services)
    {
        services.AddCommonServices();
        services.AddRoutingServices();
        services.AddCarrierServices();
        services.AddTrackingServices();
        services.AddOrderServices();
        services.AddAuditServices();
        return services;
    }
}
```

---

## Routing Registration

```csharp
// src/DtExpress.Infrastructure/DependencyInjection/RoutingRegistration.cs

namespace DtExpress.Infrastructure.DependencyInjection;

internal static class RoutingRegistration
{
    internal static IServiceCollection AddRoutingServices(this IServiceCollection services)
    {
        // === Algorithms (pure computation) ===
        services.AddSingleton<AStarPathfinder>();
        services.AddSingleton<DijkstraPathfinder>();

        // === Strategies (register each as IRouteStrategy) ===
        // OCP: Add new strategy = add ONE line here + new class
        services.AddSingleton<IRouteStrategy>(sp =>
            new FastestRouteStrategy(sp.GetRequiredService<AStarPathfinder>()));
        services.AddSingleton<IRouteStrategy>(sp =>
            new CheapestRouteStrategy(sp.GetRequiredService<DijkstraPathfinder>()));
        services.AddSingleton<IRouteStrategy>(sp =>
            new BalancedRouteStrategy(
                sp.GetRequiredService<AStarPathfinder>(),
                sp.GetRequiredService<DijkstraPathfinder>()));

        // === Factory (builds registry from IEnumerable<IRouteStrategy>) ===
        services.AddSingleton<IRouteStrategyFactory, RouteStrategyFactory>();

        // === Map service (mock) ===
        services.AddSingleton<IMapService, MockMapService>();

        // === Application services ===
        services.AddScoped<RouteCalculationService>();
        services.AddScoped<RouteComparisonService>();

        // === Cross-domain port ===
        services.AddScoped<IRoutingPort, RoutingPortAdapter>();

        return services;
    }
}
```

### How `RouteStrategyFactory` Works (No switch/if-else)

```csharp
// src/DtExpress.Infrastructure/Routing/RouteStrategyFactory.cs

public sealed class RouteStrategyFactory : IRouteStrategyFactory
{
    private readonly IReadOnlyDictionary<string, IRouteStrategy> _strategies;

    // DI injects ALL registered IRouteStrategy instances
    public RouteStrategyFactory(IEnumerable<IRouteStrategy> strategies)
    {
        _strategies = strategies
            .ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);
    }

    public IRouteStrategy Create(string strategyName)
    {
        if (_strategies.TryGetValue(strategyName, out var strategy))
            return strategy;
        throw new StrategyNotFoundException(strategyName);
    }

    public IReadOnlyList<string> Available()
        => _strategies.Keys.ToList().AsReadOnly();
}
```

> **No switch. No if-else. Dictionary lookup populated by DI.** ✅

### Decorator Composition (Optional Enhancement)

```csharp
// To add caching + logging decorators around strategies:

services.AddSingleton<IRouteStrategy>(sp =>
    new LoggingRouteDecorator(
        new CachingRouteDecorator(
            new FastestRouteStrategy(sp.GetRequiredService<AStarPathfinder>()),
            new ConcurrentDictionary<string, Route>()),
        sp.GetRequiredService<ILogger<LoggingRouteDecorator>>()));
```

---

## Carrier Registration

```csharp
// src/DtExpress.Infrastructure/DependencyInjection/CarrierRegistration.cs

namespace DtExpress.Infrastructure.DependencyInjection;

internal static class CarrierRegistration
{
    internal static IServiceCollection AddCarrierServices(this IServiceCollection services)
    {
        // === Adapters (register each as ICarrierAdapter) ===
        // OCP: Add new carrier = add ONE line here + new adapter class
        services.AddSingleton<ICarrierAdapter, SfExpressAdapter>();
        services.AddSingleton<ICarrierAdapter, JdLogisticsAdapter>();

        // === Factory (builds registry from IEnumerable<ICarrierAdapter>) ===
        services.AddSingleton<ICarrierAdapterFactory, CarrierAdapterFactory>();

        // === Selection strategy ===
        services.AddSingleton<ICarrierSelector, CheapestCarrierSelector>();

        // === Application services ===
        services.AddScoped<CarrierQuotingService>();
        services.AddScoped<CarrierBookingService>();

        // === Cross-domain port ===
        services.AddScoped<ICarrierPort, CarrierPortAdapter>();

        return services;
    }
}
```

### How `CarrierAdapterFactory` Works (No switch/if-else)

```csharp
// src/DtExpress.Infrastructure/Carrier/CarrierAdapterFactory.cs

public sealed class CarrierAdapterFactory : ICarrierAdapterFactory
{
    private readonly IReadOnlyDictionary<string, ICarrierAdapter> _adapters;

    public CarrierAdapterFactory(IEnumerable<ICarrierAdapter> adapters)
    {
        _adapters = adapters
            .ToDictionary(a => a.CarrierCode, a => a, StringComparer.OrdinalIgnoreCase);
    }

    public ICarrierAdapter Resolve(string carrierCode)
    {
        if (_adapters.TryGetValue(carrierCode, out var adapter))
            return adapter;
        throw new CarrierNotFoundException(carrierCode);
    }

    public IReadOnlyList<ICarrierAdapter> GetAll()
        => _adapters.Values.ToList().AsReadOnly();
}
```

> **Same registry pattern as routing. Consistent. No branching.** ✅

---

## Tracking Registration

```csharp
// src/DtExpress.Infrastructure/DependencyInjection/TrackingRegistration.cs

namespace DtExpress.Infrastructure.DependencyInjection;

internal static class TrackingRegistration
{
    internal static IServiceCollection AddTrackingServices(this IServiceCollection services)
    {
        // === Subject (singleton — holds all subscriptions) ===
        services.AddSingleton<ITrackingSubject, InMemoryTrackingSubject>();

        // === Sources (mock event generators) ===
        services.AddSingleton<ITrackingSource, ScriptedTrackingSource>();

        // === Observers ===
        services.AddSingleton<ITrackingObserver, ConsoleTrackingObserver>();

        // === Application services ===
        services.AddScoped<TrackingSubscriptionService>();

        // === Cross-domain port ===
        services.AddScoped<ITrackingPort, TrackingPortAdapter>();

        return services;
    }
}
```

---

## Order Registration

```csharp
// src/DtExpress.Infrastructure/DependencyInjection/OrderRegistration.cs

namespace DtExpress.Infrastructure.DependencyInjection;

internal static class OrderRegistration
{
    internal static IServiceCollection AddOrderServices(this IServiceCollection services)
    {
        // === State implementations (transient — new instance per order action) ===
        // States are NOT registered in DI. They are created by the Order aggregate or
        // by the repository when loading. States are pure logic, no dependencies.

        // === Repositories (singleton — in-memory store) ===
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        services.AddSingleton<IOrderReadService, InMemoryOrderReadService>();

        // === CQRS Command Handlers ===
        services.AddScoped<ICommandHandler<CreateOrderCommand, Guid>, CreateOrderHandler>();
        services.AddScoped<ICommandHandler<ConfirmOrderCommand, bool>, ConfirmOrderHandler>();
        services.AddScoped<ICommandHandler<ShipOrderCommand, BookingResult>, ShipOrderHandler>();
        services.AddScoped<ICommandHandler<DeliverOrderCommand, bool>, DeliverOrderHandler>();
        services.AddScoped<ICommandHandler<CancelOrderCommand, bool>, CancelOrderHandler>();

        // === CQRS Query Handlers ===
        services.AddScoped<IQueryHandler<GetOrderByIdQuery, OrderDetail?>, GetOrderByIdHandler>();
        services.AddScoped<IQueryHandler<ListOrdersQuery, IReadOnlyList<OrderSummary>>, ListOrdersHandler>();

        return services;
    }
}
```

### State Pattern — No DI for States

> States are NOT registered in DI. They are value-like objects created directly:
> - `Order` is created with `new CreatedState()` as initial state
> - `IOrderState.Transition()` returns a `new ConfirmedState()`, etc.
> - States have NO dependencies (pure logic)
> - This is intentional: states don't need DI because they have no external dependencies

---

## Audit Registration

```csharp
// src/DtExpress.Infrastructure/DependencyInjection/AuditRegistration.cs

namespace DtExpress.Infrastructure.DependencyInjection;

internal static class AuditRegistration
{
    internal static IServiceCollection AddAuditServices(this IServiceCollection services)
    {
        // === Sink (with PII masking decorator wrapping in-memory store) ===
        services.AddSingleton<InMemoryAuditSink>();
        services.AddSingleton<IAuditSink>(sp =>
            new PiiMaskingAuditDecorator(
                sp.GetRequiredService<InMemoryAuditSink>()));

        // === Query service ===
        services.AddSingleton<IAuditQueryService>(sp =>
            sp.GetRequiredService<InMemoryAuditSink>());
        // Note: InMemoryAuditSink implements both IAuditSink and IAuditQueryService

        // === Interceptor ===
        services.AddSingleton<IAuditInterceptor, DomainEventAuditInterceptor>();

        // === Cross-domain port ===
        services.AddScoped<IAuditPort, AuditPortAdapter>();

        return services;
    }
}
```

### Decorator Wiring Detail

```
Write path:  Handler → IAuditPort → AuditPortAdapter → IAuditSink
                                                            │
                                                    PiiMaskingAuditDecorator
                                                            │
                                                    InMemoryAuditSink (actual store)

Read path:   Controller → IAuditQueryService → InMemoryAuditSink (same instance)
```

---

## Common Registration

```csharp
// src/DtExpress.Infrastructure/DependencyInjection/CommonRegistration.cs (part of the files)

internal static class CommonRegistration
{
    internal static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        // === Cross-cutting ===
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();
        services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();

        // === CQRS Dispatchers ===
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // === Domain Event Publisher ===
        services.AddScoped<IDomainEventPublisher, InMemoryDomainEventPublisher>();

        return services;
    }
}
```

---

## CQRS Dispatcher Wiring

### How `CommandDispatcher` Resolves Handlers (No switch/if-else)

```csharp
// src/DtExpress.Infrastructure/Common/CommandDispatcher.cs

public sealed class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _provider;

    public CommandDispatcher(IServiceProvider provider) => _provider = provider;

    public async Task<TResult> DispatchAsync<TResult>(
        ICommand<TResult> command, CancellationToken ct = default)
    {
        // Build the handler type: ICommandHandler<ConcreteCommand, TResult>
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));

        // Resolve from DI — no switch, no if/else
        var handler = _provider.GetRequiredService(handlerType);

        // Invoke HandleAsync via reflection (or use dynamic dispatch)
        var method = handlerType.GetMethod("HandleAsync")!;
        var task = (Task<TResult>)method.Invoke(handler, new object[] { command, ct })!;
        return await task;
    }
}
```

> **Resolution is fully generic. Adding a new command = new command class + new handler class + DI line. Zero changes to dispatcher.** ✅

---

## OCP Proof: Adding New Implementations

### Scenario 1: Add a New Routing Strategy ("Eco-Friendly")

```diff
  // 1. Create new class (new file)
+ public class EcoFriendlyRouteStrategy : IRouteStrategy
+ {
+     public string Name => "EcoFriendly";
+     public Route Calculate(RouteRequest request) { /* ... */ }
+ }

  // 2. Add ONE line to DI registration
  services.AddSingleton<IRouteStrategy>(sp =>
      new FastestRouteStrategy(sp.GetRequiredService<AStarPathfinder>()));
  services.AddSingleton<IRouteStrategy>(sp =>
      new CheapestRouteStrategy(sp.GetRequiredService<DijkstraPathfinder>()));
+ services.AddSingleton<IRouteStrategy>(sp =>
+     new EcoFriendlyRouteStrategy(/* ... */));

  // 3. ZERO changes to: RouteStrategyFactory, RouteCalculationService, RoutingController
```

### Scenario 2: Add a New Carrier ("ZTO")

```diff
  // 1. Create new adapter class
+ public class ZtoExpressAdapter : ICarrierAdapter
+ {
+     public string CarrierCode => "ZTO";
+     /* implement GetQuoteAsync, BookAsync, TrackAsync */
+ }

  // 2. Add ONE line to DI
  services.AddSingleton<ICarrierAdapter, SfExpressAdapter>();
  services.AddSingleton<ICarrierAdapter, JdLogisticsAdapter>();
+ services.AddSingleton<ICarrierAdapter, ZtoExpressAdapter>();

  // 3. ZERO changes to: CarrierAdapterFactory, CarrierQuotingService, CarrierController
```

### Scenario 3: Add a New CQRS Command ("UpdateOrderAddress")

```diff
  // 1. Create command record + handler class
+ public record UpdateAddressCommand(Guid OrderId, Address NewAddress) : ICommand<bool>;
+ public class UpdateAddressHandler : ICommandHandler<UpdateAddressCommand, bool> { /* ... */ }

  // 2. Add ONE line to DI
+ services.AddScoped<ICommandHandler<UpdateAddressCommand, bool>, UpdateAddressHandler>();

  // 3. ZERO changes to: CommandDispatcher, OrdersController (just add endpoint)
```

---

## Anti-Patterns Avoided

| Anti-Pattern | How We Avoid It |
|-------------|-----------------|
| `switch(carrierCode)` for adapter selection | Registry-based factory via `Dictionary<string, ICarrierAdapter>` |
| `if(strategy == "fastest")` for strategy selection | Same registry pattern via `Dictionary<string, IRouteStrategy>` |
| `switch(commandType)` for CQRS dispatch | Generic type resolution via `IServiceProvider.GetRequiredService(handlerType)` |
| Decorator chains hardcoded in business code | Composition happens ONLY in DI registration |
| States registered as DI services | States are pure objects, created by aggregate — no DI needed |
| God-class `ServiceRegistration` | Split into per-domain registration files |
