using DtExpress.Api.Hubs;
using DtExpress.Domain.Tracking.Interfaces;
using DtExpress.Domain.Tracking.Models;
using Microsoft.AspNetCore.SignalR;

namespace DtExpress.Api.Tracking;

/// <summary>
/// Observer Pattern — Observer: pushes tracking events to connected SignalR clients.
/// Registered as a global observer on InMemoryTrackingSubject so ALL tracking events
/// are forwarded to the appropriate SignalR group (grouped by tracking number).
///
/// This lives in the Api project because it depends on the SignalR Hub context.
/// </summary>
public sealed class SignalRTrackingObserver : ITrackingObserver
{
    private readonly IHubContext<TrackingHub> _hubContext;
    private readonly ILogger<SignalRTrackingObserver> _logger;

    public SignalRTrackingObserver(
        IHubContext<TrackingHub> hubContext,
        ILogger<SignalRTrackingObserver> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task OnTrackingEventAsync(TrackingEvent evt, CancellationToken ct = default)
    {
        var dto = new TrackingEventDto
        {
            TrackingNumber = evt.TrackingNumber,
            EventType = evt.EventType.ToString(),
            NewStatus = evt.NewStatus?.ToString(),
            Location = evt.Location is not null
                ? new LocationDto(evt.Location.Latitude, evt.Location.Longitude)
                : null,
            Description = evt.Description,
            OccurredAt = evt.OccurredAt
        };

        await _hubContext.Clients
            .Group(evt.TrackingNumber)
            .SendAsync("TrackingUpdate", dto, ct);

        _logger.LogDebug("SignalR push: {TrackingNumber} -> {EventType}", evt.TrackingNumber, evt.EventType);
    }
}
