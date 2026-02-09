using DtExpress.Domain.Tracking.Models;

namespace DtExpress.Domain.Tracking.Interfaces;

/// <summary>
/// Observer Pattern: receives tracking event notifications.
/// Implementations: console logger, dashboard, notification service, etc.
/// </summary>
public interface ITrackingObserver
{
    /// <summary>Called when a tracking event occurs for a subscribed tracking number.</summary>
    Task OnTrackingEventAsync(TrackingEvent evt, CancellationToken ct = default);
}
