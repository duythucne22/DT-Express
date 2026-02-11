using System.Collections.Concurrent;
using DtExpress.Domain.Carrier.Enums;
using DtExpress.Domain.Tracking.Interfaces;
using DtExpress.Domain.Tracking.Models;

namespace DtExpress.Infrastructure.Tracking;

/// <summary>
/// Observer Pattern — Subject: manages per-tracking-number observer subscriptions
/// and distributes <see cref="TrackingEvent"/>s only to relevant observers.
/// <para>
/// Thread-safe: uses <see cref="ConcurrentDictionary{TKey,TValue}"/> for subscriptions
/// and <c>lock</c> on observer lists. Maintains a <see cref="TrackingSnapshot"/>
/// materialized view updated on every status-change event.
/// </para>
/// </summary>
public sealed class InMemoryTrackingSubject : ITrackingSubject
{
    /// <summary>Per-tracking-number observer lists. Key = trackingNumber.</summary>
    private readonly ConcurrentDictionary<string, List<ITrackingObserver>> _subscriptions = new();

    /// <summary>Global observers that receive ALL tracking events regardless of tracking number.</summary>
    private readonly List<ITrackingObserver> _globalObservers = [];
    private readonly object _globalLock = new();

    /// <summary>Latest snapshot per tracking number — materialized from events.</summary>
    private readonly ConcurrentDictionary<string, TrackingSnapshot> _snapshots = new();

    /// <inheritdoc />
    /// <remarks>
    /// Adds the observer to the list for the given tracking number.
    /// Returns an <see cref="IDisposable"/> that removes it on dispose (unsubscribe).
    /// </remarks>
    public IDisposable Subscribe(string trackingNumber, ITrackingObserver observer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);
        ArgumentNullException.ThrowIfNull(observer);

        // Special wildcard: "*" subscribes to ALL tracking events (used by SignalR bridge)
        if (trackingNumber == "*")
        {
            lock (_globalLock)
            {
                _globalObservers.Add(observer);
            }
            return new Unsubscriber(_globalObservers, observer, _globalLock);
        }

        var observers = _subscriptions.GetOrAdd(trackingNumber, _ => []);

        lock (observers)
        {
            observers.Add(observer);
        }

        return new Unsubscriber(observers, observer);
    }

    /// <inheritdoc />
    /// <remarks>
    /// 1. Updates the snapshot if the event carries a status change.
    /// 2. Notifies only observers subscribed to this event's tracking number.
    /// Observers that throw are silently skipped (fault isolation).
    /// </remarks>
    public async Task PublishAsync(TrackingEvent evt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        // 1. Update snapshot on status changes
        if (evt.EventType == Domain.Tracking.Enums.TrackingEventType.StatusChanged
            && evt.NewStatus.HasValue)
        {
            _snapshots.AddOrUpdate(
                evt.TrackingNumber,
                _ => new TrackingSnapshot(
                    evt.TrackingNumber,
                    evt.NewStatus.Value,
                    evt.Location,
                    evt.OccurredAt),
                (_, existing) => new TrackingSnapshot(
                    evt.TrackingNumber,
                    evt.NewStatus.Value,
                    evt.Location ?? existing.LastLocation,
                    evt.OccurredAt));
        }
        else if (evt.Location is not null)
        {
            // Location-only update — preserve status, update location
            _snapshots.AddOrUpdate(
                evt.TrackingNumber,
                _ => new TrackingSnapshot(
                    evt.TrackingNumber,
                    ShipmentStatus.Created,
                    evt.Location,
                    evt.OccurredAt),
                (_, existing) => existing with
                {
                    LastLocation = evt.Location,
                    UpdatedAt = evt.OccurredAt
                });
        }

        // 2. Notify per-tracking-number observers
        if (_subscriptions.TryGetValue(evt.TrackingNumber, out var observers))
        {
            ITrackingObserver[] snapshot;
            lock (observers)
            {
                snapshot = [.. observers];
            }

            foreach (var observer in snapshot)
            {
                try
                {
                    await observer.OnTrackingEventAsync(evt, ct);
                }
                catch
                {
                    // Fault isolation: one failing observer must not block others
                }
            }
        }

        // 3. Notify global observers (e.g. SignalR bridge)
        ITrackingObserver[] globalSnapshot;
        lock (_globalLock)
        {
            globalSnapshot = [.. _globalObservers];
        }

        foreach (var observer in globalSnapshot)
        {
            try
            {
                await observer.OnTrackingEventAsync(evt, ct);
            }
            catch
            {
                // Fault isolation
            }
        }
    }

    /// <inheritdoc />
    public TrackingSnapshot? GetSnapshot(string trackingNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        return _snapshots.TryGetValue(trackingNumber, out var snapshot) ? snapshot : null;
    }

    /// <summary>
    /// Disposable token returned from <see cref="Subscribe"/> that removes the
    /// observer from the subscription list when disposed.
    /// </summary>
    private sealed class Unsubscriber : IDisposable
    {
        private readonly List<ITrackingObserver> _observers;
        private readonly ITrackingObserver _observer;
        private readonly object? _externalLock;
        private bool _disposed;

        internal Unsubscriber(List<ITrackingObserver> observers, ITrackingObserver observer, object? externalLock = null)
        {
            _observers = observers;
            _observer = observer;
            _externalLock = externalLock;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            var lockObj = _externalLock ?? _observers;
            lock (lockObj)
            {
                _observers.Remove(_observer);
            }
        }
    }
}
