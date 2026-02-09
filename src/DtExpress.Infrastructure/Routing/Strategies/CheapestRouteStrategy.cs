using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Domain.Routing.Models;
using DtExpress.Domain.ValueObjects;
using DtExpress.Infrastructure.Routing.Algorithms;

namespace DtExpress.Infrastructure.Routing.Strategies;

/// <summary>
/// Strategy Pattern: optimizes routes for <strong>minimal cost</strong>.
/// <para>
/// Uses <see cref="DijkstraPathfinder"/> (cost-weighted shortest path) to find
/// the cheapest route through the logistics network. Dijkstra explores by
/// <c>Money.Amount</c>, so the result is guaranteed cost-optimal.
/// </para>
/// </summary>
public sealed class CheapestRouteStrategy : IRouteStrategy
{
    private readonly DijkstraPathfinder _pathfinder;
    private readonly IMapService _mapService;

    public CheapestRouteStrategy(DijkstraPathfinder pathfinder, IMapService mapService)
    {
        _pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder));
        _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
    }

    /// <inheritdoc />
    public string Name => "Cheapest";

    /// <inheritdoc />
    public Route Calculate(RouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Build graph from map service
        var graph = _mapService.BuildGraph(request.Origin, request.Destination);

        // 2. Find cheapest path using Dijkstra (optimizes on cost)
        var pathResult = _pathfinder.FindPath(graph, "ORIGIN", "DESTINATION");

        // 3. Convert PathResult → Route
        return new Route(
            StrategyUsed: Name,
            WaypointNodeIds: pathResult.NodeIds,
            DistanceKm: pathResult.TotalDistanceKm,
            EstimatedDuration: pathResult.TotalDuration,
            EstimatedCost: pathResult.TotalCost);
    }
}
