namespace DtExpress.Infrastructure.Data.Entities;

/// <summary>
/// EF entity mapping to the <c>carriers</c> table.
/// PK: code (VARCHAR(10)), not UUID.
/// </summary>
public sealed class CarrierEntity
{
    public string Code { get; set; } = null!; // PK: "SF", "JD"
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation: orders assigned to this carrier
    public ICollection<OrderEntity> Orders { get; set; } = new List<OrderEntity>();
    // Navigation: bookings for this carrier
    public ICollection<BookingEntity> Bookings { get; set; } = new List<BookingEntity>();
    // Navigation: quotes from this carrier
    public ICollection<CarrierQuoteEntity> Quotes { get; set; } = new List<CarrierQuoteEntity>();
}
