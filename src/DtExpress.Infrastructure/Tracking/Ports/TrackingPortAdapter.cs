using DtExpress.Application.Ports;
using DtExpress.Domain.Tracking.Interfaces;
using DtExpress.Domain.Tracking.Models;

namespace DtExpress.Infrastructure.Tracking.Ports;

/// <summary>
/// Port Adapter: bridges <see cref="ITrackingPort"/> (Application layer) to
/// Infrastructure tracking implementation via <see cref="ITrackingSubject"/>.
/// <para>
/// Used by <c>GetOrderByIdHandler</c> to enrich order details with current
/// tracking status without coupling to the Observer pattern internals.
/// </para>
/// </summary>
public sealed class TrackingPortAdapter : ITrackingPort
{
    private readonly ITrackingSubject _subject;

    public TrackingPortAdapter(ITrackingSubject subject)
    {
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reads the latest <see cref="TrackingSnapshot"/> from the subject's materialized view.
    /// Returns <c>null</c> if no events have been published for this tracking number.
    /// </remarks>
    public Task<TrackingSnapshot?> GetSnapshotAsync(string trackingNumber, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var snapshot = _subject.GetSnapshot(trackingNumber);
        return Task.FromResult(snapshot);
    }
}
