using DtExpress.Domain.Tracking.Models;

namespace DtExpress.Domain.Tracking.Interfaces;

/// <summary>
/// Observer Pattern: manages subscriptions and event distribution.
/// Subscribers receive only events for their tracking number (not a global broadcast).
/// </summary>
public interface ITrackingSubject
{
    /// <summary>Subscribe an observer to events for a specific tracking number. Returns disposable to unsubscribe.</summary>
    IDisposable Subscribe(string trackingNumber, ITrackingObserver observer);

    /// <summary>Publish an event. Notifies only observers subscribed to this event's tracking number.</summary>
    Task PublishAsync(TrackingEvent evt, CancellationToken ct = default);

    /// <summary>Get the latest known snapshot for a tracking number (for new subscribers).</summary>
    TrackingSnapshot? GetSnapshot(string trackingNumber);
}
