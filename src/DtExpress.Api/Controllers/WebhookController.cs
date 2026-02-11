using System.Security.Cryptography;
using System.Text;
using DtExpress.Api.Models;
using DtExpress.Domain.Carrier.Enums;
using DtExpress.Domain.Common;
using DtExpress.Domain.Tracking.Enums;
using DtExpress.Domain.Tracking.Interfaces;
using DtExpress.Domain.Tracking.Models;
using DtExpress.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DtExpress.Api.Controllers;

/// <summary>
/// Carrier webhook receiver: accepts inbound status updates from external carrier systems.
/// Anonymous (carriers call this) but secured with HMAC-SHA256 signature validation.
///
/// Shows: External integration, event-driven architecture, webhook security.
/// </summary>
[ApiController]
[Route("api/webhooks")]
[Produces("application/json")]
[Tags("Webhooks")]
[AllowAnonymous]
public sealed class WebhookController : ControllerBase
{
    private readonly ITrackingSubject _trackingSubject;
    private readonly ICorrelationIdProvider _correlationId;
    private readonly ILogger<WebhookController> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Expected webhook secret for HMAC-SHA256 signature validation.
    /// Configure via <c>Webhooks:Secret</c> in appsettings.json (defaults to demo secret).
    /// </summary>
    private string WebhookSecret =>
        _configuration.GetValue<string>("Webhooks:Secret") ?? "dt-express-webhook-secret-2026";

    public WebhookController(
        ITrackingSubject trackingSubject,
        ICorrelationIdProvider correlationId,
        ILogger<WebhookController> logger,
        IConfiguration configuration)
    {
        _trackingSubject = trackingSubject;
        _correlationId = correlationId;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>Receive carrier webhook for tracking status updates.</summary>
    /// <remarks>
    /// Accepts a carrier webhook payload and publishes the tracking event to the
    /// internal tracking system (Observer Pattern → SignalR push).
    ///
    /// **Security**: Requires <c>X-Webhook-Signature</c> header with HMAC-SHA256 signature:
    /// ```
    /// HMAC-SHA256(requestBody, secret)
    /// ```
    /// Secret is configured via <c>Webhooks:Secret</c> or defaults to <c>dt-express-webhook-secret-2026</c>.
    ///
    /// **Payload example**:
    /// ```json
    /// {
    ///   "trackingNumber": "SF0000000001",
    ///   "status": "InTransit",
    ///   "description": "Package arrived at sorting center",
    ///   "latitude": 31.2304,
    ///   "longitude": 121.4737,
    ///   "occurredAt": "2026-01-15T10:30:00Z"
    /// }
    /// ```
    /// </remarks>
    /// <param name="code">Carrier code (e.g. "SF" or "JD").</param>
    /// <param name="payload">Webhook payload with tracking update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Webhook accepted and tracking event published.</response>
    /// <response code="400">Invalid payload or missing fields.</response>
    /// <response code="401">Invalid or missing HMAC signature.</response>
    [HttpPost("carrier/{code}")]
    [ProducesResponseType(typeof(ApiResponse<WebhookAcceptedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CarrierWebhook(
        [FromRoute] string code,
        [FromBody] CarrierWebhookPayload payload,
        CancellationToken ct)
    {
        // 1. Validate HMAC signature
        var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(signature))
        {
            return Unauthorized(ApiResponse<object>.Fail(
                "MISSING_SIGNATURE",
                "X-Webhook-Signature header is required.",
                _correlationId.GetCorrelationId()));
        }

        // Re-read raw body for signature verification
        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);

        if (!ValidateSignature(rawBody, signature))
        {
            _logger.LogWarning("Webhook signature validation failed for carrier {CarrierCode}", code);
            return Unauthorized(ApiResponse<object>.Fail(
                "INVALID_SIGNATURE",
                "Webhook signature validation failed.",
                _correlationId.GetCorrelationId()));
        }

        // 2. Validate payload
        if (string.IsNullOrWhiteSpace(payload.TrackingNumber))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "VALIDATION_ERROR",
                "TrackingNumber is required.",
                _correlationId.GetCorrelationId()));
        }

        // 3. Parse status
        if (!Enum.TryParse<ShipmentStatus>(payload.Status, ignoreCase: true, out var shipmentStatus))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "VALIDATION_ERROR",
                $"Invalid status '{payload.Status}'. Valid values: {string.Join(", ", Enum.GetNames<ShipmentStatus>())}",
                _correlationId.GetCorrelationId()));
        }

        // 4. Build tracking event
        GeoCoordinate? location = payload.Latitude.HasValue && payload.Longitude.HasValue
            ? new GeoCoordinate(payload.Latitude.Value, payload.Longitude.Value)
            : null;

        var trackingEvent = new TrackingEvent(
            payload.TrackingNumber,
            TrackingEventType.StatusChanged,
            shipmentStatus,
            location,
            payload.Description ?? $"Carrier {code.ToUpperInvariant()} webhook: {shipmentStatus}",
            payload.OccurredAt ?? DateTimeOffset.UtcNow);

        // 5. Publish to tracking system (triggers Observer → SignalR push)
        await _trackingSubject.PublishAsync(trackingEvent, ct);

        _logger.LogInformation(
            "Webhook received from carrier {CarrierCode}: {TrackingNumber} → {Status}",
            code, payload.TrackingNumber, shipmentStatus);

        return Ok(ApiResponse<WebhookAcceptedResponse>.Ok(
            new WebhookAcceptedResponse
            {
                Accepted = true,
                TrackingNumber = payload.TrackingNumber,
                Status = shipmentStatus.ToString(),
                CarrierCode = code.ToUpperInvariant()
            },
            _correlationId.GetCorrelationId()));
    }

    // ── Signature Validation ─────────────────────────────────────

    private bool ValidateSignature(string body, string providedSignature)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var expectedSignature = Convert.ToHexString(hash).ToLowerInvariant();

        // Support both raw hex and sha256= prefix
        var cleanSignature = providedSignature
            .Replace("sha256=", "", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToLowerInvariant();

        return string.Equals(expectedSignature, cleanSignature, StringComparison.OrdinalIgnoreCase);
    }
}

// ── DTOs ─────────────────────────────────────────────────────────

/// <summary>Inbound webhook payload from carrier systems.</summary>
public sealed record CarrierWebhookPayload
{
    /// <summary>Tracking number for the shipment.</summary>
    public string TrackingNumber { get; init; } = null!;

    /// <summary>New shipment status (Created, PickedUp, InTransit, OutForDelivery, Delivered, Exception).</summary>
    public string Status { get; init; } = null!;

    /// <summary>Human-readable description of the event.</summary>
    public string? Description { get; init; }

    /// <summary>Latitude of the event location (optional).</summary>
    public decimal? Latitude { get; init; }

    /// <summary>Longitude of the event location (optional).</summary>
    public decimal? Longitude { get; init; }

    /// <summary>When the event occurred (ISO 8601). Defaults to now if omitted.</summary>
    public DateTimeOffset? OccurredAt { get; init; }
}

/// <summary>Response confirming webhook was accepted.</summary>
public sealed record WebhookAcceptedResponse
{
    public bool Accepted { get; init; }
    public string TrackingNumber { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string CarrierCode { get; init; } = null!;
}
