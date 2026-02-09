using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Routing.Models;

/// <summary>
/// A directed edge in the logistics graph — represents a transport link between two hubs.
/// Carries distance, travel time, and cost (toll/fuel).
/// </summary>
public sealed record GraphEdge(
    string FromNodeId,
    string ToNodeId,
    decimal DistanceKm,
    TimeSpan Duration,
    Money Cost);
