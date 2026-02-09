using DtExpress.Domain.Routing.Models;

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
