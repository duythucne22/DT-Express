using DtExpress.Domain.Routing.Models;

namespace DtExpress.Domain.Routing.Interfaces;

/// <summary>
/// Algorithm abstraction: pure computation, zero business logic.
/// Separated from IRouteStrategy per SRP.
/// </summary>
public interface IPathfinder
{
    /// <summary>Find optimal path through graph from origin to destination node.</summary>
    PathResult FindPath(Graph graph, string fromNodeId, string toNodeId);
}
