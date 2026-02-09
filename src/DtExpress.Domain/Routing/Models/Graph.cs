namespace DtExpress.Domain.Routing.Models;

/// <summary>
/// Directed weighted graph used by pathfinding algorithms.
/// Nodes represent logistics hubs; edges represent transport links.
/// </summary>
public sealed class Graph
{
    public IReadOnlyDictionary<string, GraphNode> Nodes { get; }
    public IReadOnlyList<GraphEdge> Edges { get; }

    public Graph(
        IReadOnlyDictionary<string, GraphNode> nodes,
        IReadOnlyList<GraphEdge> edges)
    {
        Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        Edges = edges ?? throw new ArgumentNullException(nameof(edges));
    }

    /// <summary>Get outgoing edges from a node.</summary>
    public IEnumerable<GraphEdge> GetEdgesFrom(string nodeId)
        => Edges.Where(e => e.FromNodeId == nodeId);
}
