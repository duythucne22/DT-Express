namespace DtExpress.Infrastructure.Data.Entities;

/// <summary>
/// EF entity mapping to the <c>bookings</c> table.
/// Records carrier booking confirmations.
/// </summary>
public sealed class BookingEntity
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public string CarrierCode { get; set; } = null!;
    public string TrackingNumber { get; set; } = null!;
    public DateTimeOffset BookedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────────
    public OrderEntity? Order { get; set; }
    public CarrierEntity Carrier { get; set; } = null!;
}
