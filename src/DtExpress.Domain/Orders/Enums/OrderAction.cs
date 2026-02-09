namespace DtExpress.Domain.Orders.Enums;

/// <summary>
/// Actions that can be applied to an Order aggregate via the State Pattern.
/// Each action triggers a state transition (or throws if invalid).
/// </summary>
public enum OrderAction
{
    /// <summary>确认订单 — Confirm the order (Created → Confirmed).</summary>
    Confirm,

    /// <summary>发货 — Ship the order (Confirmed → Shipped).</summary>
    Ship,

    /// <summary>签收 — Mark as delivered (Shipped → Delivered).</summary>
    Deliver,

    /// <summary>取消 — Cancel the order (Created/Confirmed → Cancelled).</summary>
    Cancel
}
