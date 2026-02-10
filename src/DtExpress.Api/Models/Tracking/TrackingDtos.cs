namespace DtExpress.Api.Models.Tracking;

// ─────────────────────────────────────────────────────────────────
//  GET /api/tracking/{trackingNo}/snapshot
// ─────────────────────────────────────────────────────────────────

/// <summary>Coordinate in tracking context.</summary>
public sealed record TrackingLocationDto(decimal Latitude, decimal Longitude);

/// <summary>Current tracking snapshot response.</summary>
public sealed record TrackingSnapshotResponse
{
    /// <summary>The tracking number being observed.</summary>
    public string TrackingNumber { get; init; } = null!;

    /// <summary>Current shipment status (e.g. InTransit, Delivered).</summary>
    public string CurrentStatus { get; init; } = null!;

    /// <summary>Last known geographic location (null if no location data).</summary>
    public TrackingLocationDto? LastLocation { get; init; }

    /// <summary>When the snapshot was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}

// ─────────────────────────────────────────────────────────────────
//  POST /api/tracking/{trackingNo}/subscribe
// ─────────────────────────────────────────────────────────────────

/// <summary>Subscription acknowledgment with optional current snapshot.</summary>
public sealed record SubscribeResponse
{
    /// <summary>Whether subscription was successfully registered.</summary>
    public bool Subscribed { get; init; }

    /// <summary>The tracking number subscribed to.</summary>
    public string TrackingNumber { get; init; } = null!;

    /// <summary>Current snapshot at time of subscription (null if no data yet).</summary>
    public TrackingSnapshotResponse? CurrentSnapshot { get; init; }
}
