namespace DtExpress.Domain.Carrier.Enums;

/// <summary>
/// Lifecycle status of a shipment from creation through final delivery.
/// Used by both Carrier and Tracking domains.
/// </summary>
public enum ShipmentStatus
{
    /// <summary>已创建 — Shipment record created, not yet picked up.</summary>
    Created,

    /// <summary>已揽收 — Package picked up by carrier.</summary>
    PickedUp,

    /// <summary>运输中 — In transit between hubs.</summary>
    InTransit,

    /// <summary>派送中 — Out for final-mile delivery.</summary>
    OutForDelivery,

    /// <summary>已签收 — Successfully delivered to recipient.</summary>
    Delivered,

    /// <summary>异常 — Delivery exception (damaged, refused, address issue).</summary>
    Exception
}
