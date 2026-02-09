using DtExpress.Domain.Carrier.Enums;
using DtExpress.Domain.Tracking.Enums;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Tracking.Models;

/// <summary>
/// An immutable tracking event — a single point-in-time observation for a shipment.
/// Published by ITrackingSource, distributed by ITrackingSubject, consumed by ITrackingObserver.
/// </summary>
public sealed record TrackingEvent(
    string TrackingNumber,
    TrackingEventType EventType,
    ShipmentStatus? NewStatus,
    GeoCoordinate? Location,
    string? Description,
    DateTimeOffset OccurredAt);
