using System.Collections.Concurrent;
using DtExpress.Domain.Tracking.Interfaces;
using DtExpress.Domain.Tracking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DtExpress.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time tracking updates.
/// Clients join a group named after the tracking number and receive push events.
///
/// **Connection**: <c>ws://localhost:5198/hubs/tracking?access_token={jwt}</c>
///
/// **Client methods invoked by server**:
/// - <c>TrackingUpdate</c> — receives TrackingEventDto on every tracking event
///
/// **Server methods callable by client**:
/// - <c>SubscribeTracking(trackingNo)</c> — join group for a tracking number
/// - <c>UnsubscribeTracking(trackingNo)</c> — leave group
/// </summary>
[Authorize]
public sealed class TrackingHub : Hub
{
    private readonly ITrackingSubject _trackingSubject;

    public TrackingHub(ITrackingSubject trackingSubject)
    {
        _trackingSubject = trackingSubject;
    }

    /// <summary>Subscribe to real-time updates for a tracking number.</summary>
    public async Task SubscribeTracking(string trackingNo)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, trackingNo);

        // Send current snapshot immediately if available
        var snapshot = _trackingSubject.GetSnapshot(trackingNo);
        if (snapshot is not null)
        {
            await Clients.Caller.SendAsync("TrackingSnapshot", new TrackingSnapshotDto
            {
                TrackingNumber = snapshot.TrackingNumber,
                CurrentStatus = snapshot.CurrentStatus.ToString(),
                LastLocation = snapshot.LastLocation is not null
                    ? new LocationDto(snapshot.LastLocation.Latitude, snapshot.LastLocation.Longitude)
                    : null,
                UpdatedAt = snapshot.UpdatedAt
            });
        }
    }

    /// <summary>Unsubscribe from tracking number updates.</summary>
    public async Task UnsubscribeTracking(string trackingNo)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, trackingNo);
    }

    public override async Task OnConnectedAsync()
    {
        var user = Context.User?.Identity?.Name ?? "anonymous";
        await base.OnConnectedAsync();
    }
}

// ── DTOs for SignalR messages ────────────────────────────────────

/// <summary>Tracking event pushed to clients.</summary>
public sealed record TrackingEventDto
{
    public string TrackingNumber { get; init; } = null!;
    public string EventType { get; init; } = null!;
    public string? NewStatus { get; init; }
    public LocationDto? Location { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}

/// <summary>Tracking snapshot sent on subscribe.</summary>
public sealed record TrackingSnapshotDto
{
    public string TrackingNumber { get; init; } = null!;
    public string CurrentStatus { get; init; } = null!;
    public LocationDto? LastLocation { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>Geo location for SignalR messages.</summary>
public sealed record LocationDto(decimal Latitude, decimal Longitude);
