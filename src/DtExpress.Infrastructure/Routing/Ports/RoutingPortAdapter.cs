using DtExpress.Application.Ports;
using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Domain.Routing.Models;

namespace DtExpress.Infrastructure.Routing.Ports;

/// <summary>
/// Port Adapter: bridges the Application layer's <see cref="IRoutingPort"/> to
/// Infrastructure's <see cref="IRouteStrategyFactory"/> and <see cref="IMapService"/>.
/// <para>
/// Default behavior: uses the "Fastest" strategy. The API controllers can use
/// <see cref="DtExpress.Application.Routing.RouteCalculationService"/> directly for
/// strategy selection — this adapter serves cross-domain callers (e.g., ShipOrderHandler)
/// who need a simple route without caring about strategy choice.
/// </para>
/// </summary>
public sealed class RoutingPortAdapter : IRoutingPort
{
    /// <summary>Default strategy used when called via the port (cross-domain).</summary>
    private const string DefaultStrategy = "Fastest";

    private readonly IRouteStrategyFactory _strategyFactory;

    public RoutingPortAdapter(IRouteStrategyFactory strategyFactory)
    {
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
    }

    /// <inheritdoc />
    public Task<Route> CalculateRouteAsync(RouteRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Resolve default strategy from factory → calculate → wrap in Task
        var strategy = _strategyFactory.Create(DefaultStrategy);
        var route = strategy.Calculate(request);

        return Task.FromResult(route);
    }
}
