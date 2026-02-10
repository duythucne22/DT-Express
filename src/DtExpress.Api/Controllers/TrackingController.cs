using DtExpress.Api.Models;
using DtExpress.Api.Models.Tracking;
using DtExpress.Application.Tracking;
using DtExpress.Domain.Common;
using DtExpress.Domain.Tracking.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DtExpress.Api.Controllers;

/// <summary>
/// Real-time tracking: snapshot retrieval and observer subscriptions.
/// Delegates to TrackingSubscriptionService (Application layer).
/// </summary>
[ApiController]
[Route("api/tracking")]
[Produces("application/json")]
[Tags("Tracking")]
public sealed class TrackingController : ControllerBase
{
    private readonly TrackingSubscriptionService _trackingService;
    private readonly ITrackingObserver _observer;
    private readonly ICorrelationIdProvider _correlationId;

    public TrackingController(
        TrackingSubscriptionService trackingService,
        ITrackingObserver observer,
        ICorrelationIdProvider correlationId)
    {
        _trackingService = trackingService;
        _observer = observer;
        _correlationId = correlationId;
    }

    /// <summary>Get the latest known tracking snapshot for a tracking number.</summary>
    /// <remarks>
    /// Returns the materialized view built from tracking events.
    /// If no events have been received for this tracking number, returns 404.
    /// </remarks>
    /// <param name="trackingNo">Tracking number (e.g. SF0000000001).</param>
    /// <response code="200">Current tracking snapshot.</response>
    /// <response code="404">No tracking data for this number.</response>
    [HttpGet("{trackingNo}/snapshot")]
    [ProducesResponseType(typeof(ApiResponse<TrackingSnapshotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public IActionResult GetSnapshot([FromRoute] string trackingNo)
    {
        var snapshot = _trackingService.GetSnapshot(trackingNo);

        if (snapshot is null)
            return NotFound(ApiResponse<object>.Fail(
                "NOT_FOUND",
                $"No tracking data for {trackingNo}",
                _correlationId.GetCorrelationId()));

        return Ok(ApiResponse<TrackingSnapshotResponse>.Ok(
            MapToSnapshotResponse(snapshot), _correlationId.GetCorrelationId()));
    }

    /// <summary>Subscribe to tracking updates for a tracking number.</summary>
    /// <remarks>
    /// Registers a console observer for the given tracking number.
    /// In production, this would upgrade to WebSocket/SSE. For demo purposes,
    /// it registers a ConsoleTrackingObserver and returns the current snapshot.
    /// </remarks>
    /// <param name="trackingNo">Tracking number to subscribe to.</param>
    /// <response code="200">Subscription registered with current snapshot.</response>
    [HttpPost("{trackingNo}/subscribe")]
    [ProducesResponseType(typeof(ApiResponse<SubscribeResponse>), StatusCodes.Status200OK)]
    public IActionResult Subscribe([FromRoute] string trackingNo)
    {
        // Register the console observer for this tracking number
        _trackingService.Subscribe(trackingNo, _observer);

        var snapshot = _trackingService.GetSnapshot(trackingNo);

        var response = new SubscribeResponse
        {
            Subscribed = true,
            TrackingNumber = trackingNo,
            CurrentSnapshot = snapshot is not null ? MapToSnapshotResponse(snapshot) : null
        };

        return Ok(ApiResponse<SubscribeResponse>.Ok(response, _correlationId.GetCorrelationId()));
    }

    // ── Mapping helpers ──────────────────────────────────────────

    private static TrackingSnapshotResponse MapToSnapshotResponse(
        Domain.Tracking.Models.TrackingSnapshot snapshot)
    {
        return new TrackingSnapshotResponse
        {
            TrackingNumber = snapshot.TrackingNumber,
            CurrentStatus = snapshot.CurrentStatus.ToString(),
            LastLocation = snapshot.LastLocation is not null
                ? new TrackingLocationDto(snapshot.LastLocation.Latitude, snapshot.LastLocation.Longitude)
                : null,
            UpdatedAt = snapshot.UpdatedAt
        };
    }
}
