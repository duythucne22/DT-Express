namespace DtExpress.Infrastructure.Data.Entities;

/// <summary>
/// EF entity mapping to the <c>orders</c> table.
/// Value objects (ContactInfo, Address) are flattened into scalar columns.
/// </summary>
public sealed class OrderEntity
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = null!;

    // ── ContactInfo value object (flattened) ─────────────────────
    public string CustomerName { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;
    public string? CustomerEmail { get; set; }

    // ── Address value object: Origin (flattened) ─────────────────
    public string OriginStreet { get; set; } = null!;
    public string OriginCity { get; set; } = null!;
    public string OriginProvince { get; set; } = null!;
    public string OriginPostalCode { get; set; } = null!;
    public string OriginCountry { get; set; } = null!;

    // ── Address value object: Destination (flattened) ────────────
    public string DestStreet { get; set; } = null!;
    public string DestCity { get; set; } = null!;
    public string DestProvince { get; set; } = null!;
    public string DestPostalCode { get; set; } = null!;
    public string DestCountry { get; set; } = null!;

    // ── Enums (stored as string) ────────────────────────────────
    public string ServiceLevel { get; set; } = null!;  // Express, Standard, Economy
    public string Status { get; set; } = null!;         // Created, Confirmed, Shipped, Delivered, Cancelled

    // ── Carrier assignment ──────────────────────────────────────
    public string? TrackingNumber { get; set; }
    public string? CarrierCode { get; set; }

    // ── User context (nullable until Phase 8 auth) ──────────────
    public Guid? UserId { get; set; }

    // ── Timestamps ──────────────────────────────────────────────
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // ── Navigation properties ───────────────────────────────────
    public CarrierEntity? Carrier { get; set; }
    public UserEntity? User { get; set; }
    public ICollection<OrderItemEntity> Items { get; set; } = new List<OrderItemEntity>();
    public ICollection<OrderEventEntity> Events { get; set; } = new List<OrderEventEntity>();
    public ICollection<BookingEntity> Bookings { get; set; } = new List<BookingEntity>();
}
