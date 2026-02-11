namespace DtExpress.Infrastructure.Data.Entities;

/// <summary>
/// EF entity mapping to the <c>order_items</c> table.
/// Weight and Dimension value objects are flattened.
/// </summary>
public sealed class OrderItemEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }

    public string Description { get; set; } = null!;
    public int Quantity { get; set; }

    // ── Weight value object (flattened) ─────────────────────────
    public decimal WeightValue { get; set; }
    public string WeightUnit { get; set; } = null!; // Kg, G, Jin, Lb

    // ── Dimension value object (flattened, all-or-nothing nullable) ──
    public decimal? DimLengthCm { get; set; }
    public decimal? DimWidthCm { get; set; }
    public decimal? DimHeightCm { get; set; }

    // ── Navigation ──────────────────────────────────────────────
    public OrderEntity Order { get; set; } = null!;
}
