using DtExpress.Domain.Carrier.Enums;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Tracking.Models;

/// <summary>
/// Latest known state of a tracked shipment — a materialized view built from events.
/// Returned by ITrackingSubject.GetSnapshot() for new subscribers.
/// </summary>
public sealed record TrackingSnapshot(
    string TrackingNumber,
    ShipmentStatus CurrentStatus,
    GeoCoordinate? LastLocation,
    DateTimeOffset UpdatedAt);
