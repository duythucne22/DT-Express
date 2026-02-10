using System.ComponentModel.DataAnnotations;

namespace DtExpress.Api.Models.Routing;

// ─────────────────────────────────────────────────────────────────
//  Shared sub-DTOs
// ─────────────────────────────────────────────────────────────────

/// <summary>Latitude/Longitude coordinate pair.</summary>
public sealed record GeoCoordinateDto(
    [property: Required] decimal Latitude,
    [property: Required] decimal Longitude);

/// <summary>Package weight with unit.</summary>
public sealed record WeightDto(
    [property: Required, Range(0.01, double.MaxValue)] decimal Value,
    [property: Required] string Unit);

/// <summary>Money amount with currency.</summary>
public sealed record MoneyDto(decimal Amount, string Currency);

// ─────────────────────────────────────────────────────────────────
//  POST /api/routing/calculate
// ─────────────────────────────────────────────────────────────────

/// <summary>Request body for route calculation with a specific strategy.</summary>
public sealed record CalculateRouteRequest
{
    /// <summary>Origin coordinate (e.g. Shanghai: 31.2304, 121.4737).</summary>
    [Required] public GeoCoordinateDto Origin { get; init; } = null!;

    /// <summary>Destination coordinate (e.g. Beijing: 39.9042, 116.4074).</summary>
    [Required] public GeoCoordinateDto Destination { get; init; } = null!;

    /// <summary>Package weight with unit (Kg, G, Jin, Lb).</summary>
    [Required] public WeightDto PackageWeight { get; init; } = null!;

    /// <summary>Delivery speed tier: Express, Standard, Economy.</summary>
    [Required] public string ServiceLevel { get; init; } = null!;

    /// <summary>Strategy name: Fastest, Cheapest, Balanced.</summary>
    [Required] public string Strategy { get; init; } = null!;
}

// ─────────────────────────────────────────────────────────────────
//  POST /api/routing/compare
// ─────────────────────────────────────────────────────────────────

/// <summary>Request body for comparing all strategies (same as calculate minus strategy).</summary>
public sealed record CompareRoutesRequest
{
    /// <summary>Origin coordinate.</summary>
    [Required] public GeoCoordinateDto Origin { get; init; } = null!;

    /// <summary>Destination coordinate.</summary>
    [Required] public GeoCoordinateDto Destination { get; init; } = null!;

    /// <summary>Package weight with unit.</summary>
    [Required] public WeightDto PackageWeight { get; init; } = null!;

    /// <summary>Delivery speed tier: Express, Standard, Economy.</summary>
    [Required] public string ServiceLevel { get; init; } = null!;
}

// ─────────────────────────────────────────────────────────────────
//  Response DTOs
// ─────────────────────────────────────────────────────────────────

/// <summary>Calculated route result.</summary>
public sealed record RouteResponse
{
    /// <summary>Strategy that produced this route.</summary>
    public string StrategyUsed { get; init; } = null!;

    /// <summary>Ordered list of waypoint node IDs along the route.</summary>
    public IReadOnlyList<string> WaypointNodeIds { get; init; } = [];

    /// <summary>Total route distance in kilometers.</summary>
    public decimal DistanceKm { get; init; }

    /// <summary>Estimated travel duration (ISO 8601 duration or HH:mm:ss).</summary>
    public string EstimatedDuration { get; init; } = null!;

    /// <summary>Estimated shipping cost.</summary>
    public MoneyDto EstimatedCost { get; init; } = null!;
}
