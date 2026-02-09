using DtExpress.Domain.Carrier.Enums;

namespace DtExpress.Domain.Carrier.Models;

/// <summary>
/// Current tracking state for a shipment as reported by the carrier.
/// </summary>
public sealed record CarrierTrackingInfo(
    string TrackingNumber,
    ShipmentStatus Status,
    string? CurrentLocation,
    DateTimeOffset UpdatedAt);
