using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Domain.Routing.Models;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Infrastructure.Routing.Algorithms;

/// <summary>
/// A* pathfinding algorithm — optimizes for shortest distance using a heuristic.
/// <para>
/// Uses straight-line distance (<see cref="GeoCoordinate.DistanceToKm"/>) as the
/// admissible heuristic, guaranteeing optimality. Ideal for the "Fastest" route strategy.
/// </para>
/// </summary>
public sealed class AStarPathfinder : IPathfinder
{
    /// <inheritdoc />
    public PathResult FindPath(Graph graph, string fromNodeId, string toNodeId)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromNodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toNodeId);

        // Handle invalid node IDs gracefully
        if (!graph.Nodes.ContainsKey(fromNodeId) || !graph.Nodes.ContainsKey(toNodeId))
            return EmptyResult();

        // Same node — trivial path
        if (fromNodeId == toNodeId)
            return new PathResult([fromNodeId], 0m, TimeSpan.Zero, Money.Zero);

        // A* data structures
        var openSet = new PriorityQueue<string, decimal>();
        var cameFrom = new Dictionary<string, string>();
        var gScore = new Dictionary<string, decimal> { [fromNodeId] = 0m };
        var closedSet = new HashSet<string>();

        openSet.Enqueue(fromNodeId, Heuristic(graph, fromNodeId, toNodeId));

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            // Goal reached — reconstruct path
            if (current == toNodeId)
                return ReconstructPath(cameFrom, current, graph, fromNodeId);

            // Skip already-evaluated nodes
            if (!closedSet.Add(current))
                continue;

            foreach (var edge in graph.GetEdgesFrom(current))
            {
                if (closedSet.Contains(edge.ToNodeId))
                    continue;

                var tentativeG = gScore[current] + edge.DistanceKm;

                if (!gScore.TryGetValue(edge.ToNodeId, out var existingG) || tentativeG < existingG)
                {
                    cameFrom[edge.ToNodeId] = current;
                    gScore[edge.ToNodeId] = tentativeG;

                    var fScore = tentativeG + Heuristic(graph, edge.ToNodeId, toNodeId);
                    openSet.Enqueue(edge.ToNodeId, fScore);
                }
            }
        }

        // No path found
        return EmptyResult();
    }

    /// <summary>
    /// Admissible heuristic: straight-line distance between two nodes.
    /// Never overestimates, so A* remains optimal.
    /// </summary>
    private static decimal Heuristic(Graph graph, string fromId, string toId)
    {
        var fromNode = graph.Nodes[fromId];
        var toNode = graph.Nodes[toId];
        return fromNode.Coordinate.DistanceToKm(toNode.Coordinate);
    }

    /// <summary>
    /// Walk the cameFrom chain backwards to rebuild the path, then sum edge metrics.
    /// </summary>
    private static PathResult ReconstructPath(
        Dictionary<string, string> cameFrom, string current, Graph graph, string startId)
    {
        var path = new List<string> { current };

        while (cameFrom.TryGetValue(current, out var previous))
        {
            path.Add(previous);
            current = previous;
        }

        path.Reverse();

        return CalculatePathMetrics(path, graph);
    }

    /// <summary>
    /// Sum distance, duration, and cost by following edges along the path.
    /// </summary>
    private static PathResult CalculatePathMetrics(List<string> path, Graph graph)
    {
        var totalDistance = 0m;
        var totalDuration = TimeSpan.Zero;
        var totalCost = Money.Zero;

        // Build an edge lookup for O(1) access: (from, to) → edge
        var edgeLookup = graph.Edges
            .GroupBy(e => e.FromNodeId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(e => e.ToNodeId));

        for (var i = 0; i < path.Count - 1; i++)
        {
            if (edgeLookup.TryGetValue(path[i], out var toEdges)
                && toEdges.TryGetValue(path[i + 1], out var edge))
            {
                totalDistance += edge.DistanceKm;
                totalDuration += edge.Duration;
                totalCost = totalCost.Add(edge.Cost);
            }
        }

        return new PathResult(path.AsReadOnly(), totalDistance, totalDuration, totalCost);
    }

    private static PathResult EmptyResult()
        => new(Array.Empty<string>(), 0m, TimeSpan.Zero, Money.Zero);
}
