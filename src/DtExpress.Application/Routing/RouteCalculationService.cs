using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Domain.Routing.Models;

namespace DtExpress.Application.Routing;

/// <summary>
/// Application service: orchestrates strategy selection and route calculation.
/// Injected directly into RoutingController — not behind an interface.
/// </summary>
public sealed class RouteCalculationService
{
    private readonly IRouteStrategyFactory _strategyFactory;

    public RouteCalculationService(IRouteStrategyFactory strategyFactory)
    {
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
    }

    /// <summary>Calculate a route using the named strategy.</summary>
    public Route Calculate(string strategyName, RouteRequest request)
    {
        var strategy = _strategyFactory.Create(strategyName);
        return strategy.Calculate(request);
    }

    /// <summary>List all available strategy names for discovery.</summary>
    public IReadOnlyList<string> GetAvailableStrategies()
    {
        return _strategyFactory.Available();
    }
}
