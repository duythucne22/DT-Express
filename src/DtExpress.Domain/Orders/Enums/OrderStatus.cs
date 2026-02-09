namespace DtExpress.Domain.Orders.Enums;

/// <summary>
/// Order lifecycle status. Maps 1-to-1 with IOrderState implementations.
/// ADR-007: 4 forward states + 1 terminal (Cancelled).
/// </summary>
public enum OrderStatus
{
    /// <summary>已创建 — Order placed, awaiting confirmation.</summary>
    Created,

    /// <summary>已确认 — Order confirmed, ready for carrier booking.</summary>
    Confirmed,

    /// <summary>已发货 — Shipped with a carrier, tracking number assigned.</summary>
    Shipped,

    /// <summary>已签收 — Successfully delivered to recipient.</summary>
    Delivered,

    /// <summary>已取消 — Terminal state, no further transitions allowed.</summary>
    Cancelled
}
