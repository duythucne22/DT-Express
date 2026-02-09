using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Domain.Routing.Models;

namespace DtExpress.Application.Routing;

/// <summary>
/// Application service: runs all registered strategies for the same request,
/// returning a comparison list. Used by POST /api/routing/compare.
/// </summary>
public sealed class RouteComparisonService
{
    private readonly IRouteStrategyFactory _strategyFactory;

    public RouteComparisonService(IRouteStrategyFactory strategyFactory)
    {
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
    }

    /// <summary>Calculate routes using every registered strategy for comparison.</summary>
    public IReadOnlyList<Route> CompareAll(RouteRequest request)
    {
        var results = new List<Route>();

        foreach (var name in _strategyFactory.Available())
        {
            var strategy = _strategyFactory.Create(name);
            results.Add(strategy.Calculate(request));
        }

        return results.AsReadOnly();
    }
}
