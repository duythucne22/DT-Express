namespace DtExpress.Domain.Tracking.Enums;

/// <summary>
/// Type of tracking event produced by a tracking source.
/// </summary>
public enum TrackingEventType
{
    /// <summary>状态变更 — Shipment status changed (e.g. InTransit → OutForDelivery).</summary>
    StatusChanged,

    /// <summary>位置更新 — GPS/scan location updated without status change.</summary>
    LocationUpdated
}
