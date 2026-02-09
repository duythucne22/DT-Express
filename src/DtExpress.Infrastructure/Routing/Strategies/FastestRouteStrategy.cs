using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Domain.Routing.Models;
using DtExpress.Domain.ValueObjects;
using DtExpress.Infrastructure.Routing.Algorithms;

namespace DtExpress.Infrastructure.Routing.Strategies;

/// <summary>
/// Strategy Pattern: optimizes routes for <strong>minimal travel time</strong>.
/// <para>
/// Uses <see cref="AStarPathfinder"/> (heuristic-guided) to find the fastest path,
/// then wraps the result as a <see cref="Route"/>. The A* heuristic (straight-line
/// distance) ensures time-optimal pathfinding through the logistics graph.
/// </para>
/// </summary>
public sealed class FastestRouteStrategy : IRouteStrategy
{
    private readonly AStarPathfinder _pathfinder;
    private readonly IMapService _mapService;

    public FastestRouteStrategy(AStarPathfinder pathfinder, IMapService mapService)
    {
        _pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder));
        _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
    }

    /// <inheritdoc />
    public string Name => "Fastest";

    /// <inheritdoc />
    public Route Calculate(RouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Build graph from map service
        var graph = _mapService.BuildGraph(request.Origin, request.Destination);

        // 2. Find fastest path using A* (optimizes on distance → correlates with time)
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
