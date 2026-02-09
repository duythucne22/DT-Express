using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Routing.Models;

/// <summary>
/// Input contract for route calculation.
/// Validation is enforced by the value objects themselves.
/// </summary>
public sealed record RouteRequest(
    GeoCoordinate Origin,
    GeoCoordinate Destination,
    Weight PackageWeight,
    ServiceLevel ServiceLevel);
