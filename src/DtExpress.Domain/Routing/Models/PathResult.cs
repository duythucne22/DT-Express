using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Routing.Models;

/// <summary>
/// Output of a pathfinding algorithm — the optimal path with aggregated metrics.
/// </summary>
public sealed record PathResult(
    IReadOnlyList<string> NodeIds,
    decimal TotalDistanceKm,
    TimeSpan TotalDuration,
    Money TotalCost);
