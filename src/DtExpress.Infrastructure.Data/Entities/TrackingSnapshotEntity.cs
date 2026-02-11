namespace DtExpress.Infrastructure.Data.Entities;

/// <summary>
/// EF entity mapping to the <c>tracking_snapshots</c> table.
/// Materialized latest state — one row per shipment.
/// PK: tracking_number (string, not UUID).
/// </summary>
public sealed class TrackingSnapshotEntity
{
    public string TrackingNumber { get; set; } = null!; // PK

    public string CurrentStatus { get; set; } = null!; // ShipmentStatus enum as string

    // ── GeoCoordinate value object (nullable pair) ──────────────
    public decimal? LastLocationLat { get; set; }
    public decimal? LastLocationLng { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
