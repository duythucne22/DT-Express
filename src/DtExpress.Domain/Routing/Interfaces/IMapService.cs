using DtExpress.Domain.Routing.Models;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Routing.Interfaces;

/// <summary>
/// Infrastructure abstraction: provides graph data for pathfinding.
/// In production: calls map API. Here: returns hardcoded mock graph.
/// </summary>
public interface IMapService
{
    /// <summary>Build a navigation graph between two coordinates.</summary>
    Graph BuildGraph(GeoCoordinate origin, GeoCoordinate destination);
}
