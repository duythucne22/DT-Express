using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Routing.Models;

/// <summary>
/// A node in the logistics graph — represents a hub or transfer center.
/// Example: "SH-01" → "上海转运中心" at (31.23, 121.47).
/// </summary>
public sealed record GraphNode(
    string Id,
    string Name,
    GeoCoordinate Coordinate);
