using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Domain.Routing.Models;
using DtExpress.Domain.ValueObjects;
using DtExpress.Infrastructure.Routing.Algorithms;

namespace DtExpress.Infrastructure.Routing.Strategies;

/// <summary>
/// Strategy Pattern: optimizes routes for a <strong>balanced trade-off</strong> between
/// time (60% weight) and cost (40% weight).
/// <para>
/// Runs both <see cref="AStarPathfinder"/> (time-optimized) and
/// <see cref="DijkstraPathfinder"/> (cost-optimized), then uses
/// <see cref="WeightedScoreCalculator"/> to select the best compromise.
/// If both paths are identical, returns the time-optimized one.
/// </para>
/// </summary>
public sealed class BalancedRouteStrategy : IRouteStrategy
{
    private readonly AStarPathfinder _aStarPathfinder;
    private readonly DijkstraPathfinder _dijkstraPathfinder;
    private readonly IMapService _mapService;

    public BalancedRouteStrategy(
        AStarPathfinder aStarPathfinder,
        DijkstraPathfinder dijkstraPathfinder,
        IMapService mapService)
    {
        _aStarPathfinder = aStarPathfinder ?? throw new ArgumentNullException(nameof(aStarPathfinder));
        _dijkstraPathfinder = dijkstraPathfinder ?? throw new ArgumentNullException(nameof(dijkstraPathfinder));
        _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
    }

    /// <inheritdoc />
    public string Name => "Balanced";

    /// <inheritdoc />
    public Route Calculate(RouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Build graph
        var graph = _mapService.BuildGraph(request.Origin, request.Destination);

        // 2. Run both algorithms
        var fastestPath = _aStarPathfinder.FindPath(graph, "ORIGIN", "DESTINATION");
        var cheapestPath = _dijkstraPathfinder.FindPath(graph, "ORIGIN", "DESTINATION");

        // 3. If either path is empty, fall back to the other
        if (fastestPath.NodeIds.Count == 0 && cheapestPath.NodeIds.Count == 0)
            return new Route(Name, Array.Empty<string>(), 0m, TimeSpan.Zero, Money.Zero);

        if (fastestPath.NodeIds.Count == 0)
            return ToRoute(cheapestPath);

        if (cheapestPath.NodeIds.Count == 0)
            return ToRoute(fastestPath);

        // 4. Create a baseline from the worse metrics of both paths for normalization
        var baseline = new PathResult(
            Array.Empty<string>(),
            Math.Max(fastestPath.TotalDistanceKm, cheapestPath.TotalDistanceKm),
            fastestPath.TotalDuration > cheapestPath.TotalDuration
                ? fastestPath.TotalDuration
                : cheapestPath.TotalDuration,
            fastestPath.TotalCost.Amount > cheapestPath.TotalCost.Amount
                ? fastestPath.TotalCost
                : cheapestPath.TotalCost);

        // 5. Score both candidates against the baseline (60% time, 40% cost)
        var bestPath = WeightedScoreCalculator.SelectBest(
            [fastestPath, cheapestPath],
            baseline);

        return ToRoute(bestPath ?? fastestPath);
    }

    private Route ToRoute(PathResult pathResult)
        => new(
            StrategyUsed: Name,
            WaypointNodeIds: pathResult.NodeIds,
            DistanceKm: pathResult.TotalDistanceKm,
            EstimatedDuration: pathResult.TotalDuration,
            EstimatedCost: pathResult.TotalCost);
}
