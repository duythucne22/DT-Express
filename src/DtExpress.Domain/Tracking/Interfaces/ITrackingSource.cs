namespace DtExpress.Domain.Tracking.Interfaces;

/// <summary>
/// Event source: produces tracking events (mock GPS, carrier webhooks, etc.).
/// Publishes into ITrackingSubject.
/// </summary>
public interface ITrackingSource
{
    /// <summary>Descriptive name of this source.</summary>
    string Name { get; }

    /// <summary>Start producing events. Runs until cancellation.</summary>
    Task StartAsync(CancellationToken ct = default);
}
