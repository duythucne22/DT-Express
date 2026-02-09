using DtExpress.Domain.Tracking.Interfaces;
using DtExpress.Domain.Tracking.Models;

namespace DtExpress.Application.Tracking;

/// <summary>
/// Application service: manages tracking subscriptions and snapshot retrieval.
/// Wraps ITrackingSubject for use by TrackingController.
/// </summary>
public sealed class TrackingSubscriptionService
{
    private readonly ITrackingSubject _subject;

    public TrackingSubscriptionService(ITrackingSubject subject)
    {
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
    }

    /// <summary>Subscribe an observer to tracking events for a specific tracking number.</summary>
    public IDisposable Subscribe(string trackingNumber, ITrackingObserver observer)
    {
        return _subject.Subscribe(trackingNumber, observer);
    }

    /// <summary>Get the latest known snapshot for a tracking number.</summary>
    public TrackingSnapshot? GetSnapshot(string trackingNumber)
    {
        return _subject.GetSnapshot(trackingNumber);
    }
}
