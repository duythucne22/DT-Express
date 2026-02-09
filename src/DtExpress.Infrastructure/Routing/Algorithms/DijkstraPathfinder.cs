using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Domain.Routing.Models;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Infrastructure.Routing.Algorithms;

/// <summary>
/// Dijkstra's shortest-path algorithm — optimizes for minimal cost (no heuristic).
/// <para>
/// Explores all reachable nodes by increasing cost, guaranteeing the cheapest path.
/// Ideal for the "Cheapest" route strategy. Uses <see cref="PriorityQueue{TElement,TPriority}"/>
/// for efficient minimum extraction.
/// </para>
/// </summary>
public sealed class DijkstraPathfinder : IPathfinder
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

        // Dijkstra data structures
        var distances = new Dictionary<string, decimal>();
        var previous = new Dictionary<string, string?>();
        var visited = new HashSet<string>();
        var queue = new PriorityQueue<string, decimal>();

        // Initialize all nodes with infinite distance
        foreach (var nodeId in graph.Nodes.Keys)
        {
            distances[nodeId] = decimal.MaxValue;
            previous[nodeId] = null;
        }

        distances[fromNodeId] = 0m;
        queue.Enqueue(fromNodeId, 0m);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // Skip already-visited nodes (PriorityQueue may contain stale entries)
            if (!visited.Add(current))
                continue;

            // Goal reached — build result
            if (current == toNodeId)
                return BuildPathResult(previous, toNodeId, graph, fromNodeId);

            // Unreachable (remaining nodes at infinite distance)
            if (distances[current] == decimal.MaxValue)
                break;

            foreach (var edge in graph.GetEdgesFrom(current))
            {
                if (visited.Contains(edge.ToNodeId))
                    continue;

                // Dijkstra optimizes on cost (Money.Amount) for cheapest path
                var alt = distances[current] + edge.Cost.Amount;

                if (alt < distances[edge.ToNodeId])
                {
                    distances[edge.ToNodeId] = alt;
                    previous[edge.ToNodeId] = current;
                    queue.Enqueue(edge.ToNodeId, alt);
                }
            }
        }

        // No path found
        return EmptyResult();
    }

    /// <summary>
    /// Walk the previous-node chain backwards to rebuild the path, then sum edge metrics.
    /// </summary>
    private static PathResult BuildPathResult(
        Dictionary<string, string?> previous, string target, Graph graph, string startId)
    {
        var path = new List<string>();
        string? current = target;

        while (current is not null)
        {
            path.Add(current);
            current = previous.GetValueOrDefault(current);
        }

        path.Reverse();

        // Validate the reconstructed path starts at the origin
        if (path.Count == 0 || path[0] != startId)
            return EmptyResult();

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
