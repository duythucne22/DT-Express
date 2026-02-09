using DtExpress.Application.Ports;
using DtExpress.Application.Routing;
using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Infrastructure.Routing;
using DtExpress.Infrastructure.Routing.Algorithms;
using DtExpress.Infrastructure.Routing.Ports;
using DtExpress.Infrastructure.Routing.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace DtExpress.Infrastructure.DependencyInjection;

/// <summary>
/// Registers routing domain services: algorithms, strategies, factory, and port adapter.
/// <para>
/// <strong>OCP</strong>: Adding a new strategy = new class + ONE line here.
/// Zero changes to <c>RouteStrategyFactory</c>, <c>RouteCalculationService</c>, or controllers.
/// </para>
/// </summary>
internal static class RoutingRegistration
{
    internal static IServiceCollection AddRoutingServices(this IServiceCollection services)
    {
        // === Algorithms (pure computation — singleton) ===
        services.AddSingleton<AStarPathfinder>();
        services.AddSingleton<DijkstraPathfinder>();

        // === Map service (mock) ===
        services.AddSingleton<IMapService, MockMapService>();

        // === Strategies (register each as IRouteStrategy) ===
        // OCP: Add new strategy = add ONE line here + new class
        services.AddSingleton<IRouteStrategy>(sp =>
            new FastestRouteStrategy(
                sp.GetRequiredService<AStarPathfinder>(),
                sp.GetRequiredService<IMapService>()));

        services.AddSingleton<IRouteStrategy>(sp =>
            new CheapestRouteStrategy(
                sp.GetRequiredService<DijkstraPathfinder>(),
                sp.GetRequiredService<IMapService>()));

        services.AddSingleton<IRouteStrategy>(sp =>
            new BalancedRouteStrategy(
                sp.GetRequiredService<AStarPathfinder>(),
                sp.GetRequiredService<DijkstraPathfinder>(),
                sp.GetRequiredService<IMapService>()));

        // === Factory (builds registry from IEnumerable<IRouteStrategy>) ===
        services.AddSingleton<IRouteStrategyFactory, RouteStrategyFactory>();

        // === Application services ===
        services.AddScoped<RouteCalculationService>();
        services.AddScoped<RouteComparisonService>();

        // === Cross-domain port ===
        services.AddScoped<IRoutingPort, RoutingPortAdapter>();

        return services;
    }
}
