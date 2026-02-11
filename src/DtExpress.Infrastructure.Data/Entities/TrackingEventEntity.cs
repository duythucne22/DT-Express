namespace DtExpress.Infrastructure.Data.Entities;

/// <summary>
/// EF entity mapping to the <c>tracking_events</c> table.
/// Append-only event stream for shipment tracking.
/// </summary>
public sealed class TrackingEventEntity
{
    public Guid Id { get; set; }
    public string TrackingNumber { get; set; } = null!;

    public string EventType { get; set; } = null!;  // StatusChanged, LocationUpdated
    public string? NewStatus { get; set; }            // Created, PickedUp, InTransit, OutForDelivery, Delivered, Exception

    // ── GeoCoordinate value object (nullable pair) ──────────────
    public decimal? LocationLat { get; set; }
    public decimal? LocationLng { get; set; }

    public string? Description { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
