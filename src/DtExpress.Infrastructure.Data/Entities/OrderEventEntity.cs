namespace DtExpress.Infrastructure.Data.Entities;

/// <summary>
/// EF entity mapping to the <c>order_events</c> table.
/// Append-only state transition log (Event Sourcing Lite).
/// </summary>
public sealed class OrderEventEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }

    public string PreviousStatus { get; set; } = null!; // Created, Confirmed, Shipped, Delivered, Cancelled
    public string NewStatus { get; set; } = null!;       // Created, Confirmed, Shipped, Delivered, Cancelled
    public string Action { get; set; } = null!;           // Confirm, Ship, Deliver, Cancel

    public DateTimeOffset OccurredAt { get; set; }

    // ── Navigation ──────────────────────────────────────────────
    public OrderEntity Order { get; set; } = null!;
}
