using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Routing.Models;

/// <summary>
/// Output of a route calculation — the chosen path with aggregated metrics.
/// </summary>
public sealed record Route(
    string StrategyUsed,
    IReadOnlyList<string> WaypointNodeIds,
    decimal DistanceKm,
    TimeSpan EstimatedDuration,
    Money EstimatedCost);
