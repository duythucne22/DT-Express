using DtExpress.Domain.Tracking.Models;

namespace DtExpress.Application.Ports;

/// <summary>Cross-domain port: Orders → Tracking. Used by GetOrderByIdHandler.</summary>
public interface ITrackingPort
{
    Task<TrackingSnapshot?> GetSnapshotAsync(string trackingNumber, CancellationToken ct = default);
}
