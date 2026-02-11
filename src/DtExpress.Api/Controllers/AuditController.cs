using DtExpress.Api.Models;
using DtExpress.Api.Models.Audit;
using DtExpress.Domain.Audit.Interfaces;
using DtExpress.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DtExpress.Api.Controllers;

/// <summary>
/// Audit trail queries: by entity (timeline view) and by correlation ID (request tracing).
/// Read-only — all audit writes happen automatically via IAuditPort in command handlers.
/// </summary>
[ApiController]
[Route("api/audit")]
[Produces("application/json")]
[Tags("Audit")]
[Authorize(Roles = "Admin,Dispatcher")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditQueryService _auditQuery;
    private readonly ICorrelationIdProvider _correlationId;

    public AuditController(
        IAuditQueryService auditQuery,
        ICorrelationIdProvider correlationId)
    {
        _auditQuery = auditQuery;
        _correlationId = correlationId;
    }

    /// <summary>Get audit trail for a specific entity.</summary>
    /// <remarks>
    /// Returns all audit records for the given entity type and ID, ordered chronologically.
    /// Useful for viewing the complete history of an order, route, or carrier interaction.
    ///
    /// Example: GET /api/audit/entity/Order/a1b2c3d4-e5f6-7890-abcd-ef1234567890
    /// </remarks>
    /// <param name="entityType">Entity type (e.g. "Order", "Route", "Carrier").</param>
    /// <param name="entityId">Entity identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Audit trail for the entity.</response>
    [HttpGet("entity/{entityType}/{entityId}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuditRecordResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEntity(
        [FromRoute] string entityType,
        [FromRoute] string entityId,
        CancellationToken ct)
    {
        var records = await _auditQuery.GetByEntityAsync(entityType, entityId, ct);

        var response = records.Select(MapToAuditRecordResponse).ToList();

        return Ok(ApiResponse<IReadOnlyList<AuditRecordResponse>>.Ok(
            response, _correlationId.GetCorrelationId()));
    }

    /// <summary>Get all audit records for a correlation ID.</summary>
    /// <remarks>
    /// Returns all audit records that share the same correlation ID, enabling
    /// end-to-end request tracing across order creation, routing, and carrier booking.
    ///
    /// Example: GET /api/audit/correlation/req-001
    /// </remarks>
    /// <param name="correlationId">The correlation ID to search for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Audit records for the correlation ID.</response>
    [HttpGet("correlation/{correlationId}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuditRecordResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCorrelation(
        [FromRoute] string correlationId,
        CancellationToken ct)
    {
        var records = await _auditQuery.GetByCorrelationAsync(correlationId, ct);

        var response = records.Select(MapToAuditRecordResponse).ToList();

        return Ok(ApiResponse<IReadOnlyList<AuditRecordResponse>>.Ok(
            response, _correlationId.GetCorrelationId()));
    }

    // ── Mapping helpers ──────────────────────────────────────────

    private static AuditRecordResponse MapToAuditRecordResponse(Domain.Audit.Models.AuditRecord record)
    {
        return new AuditRecordResponse
        {
            Id = record.Id,
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            Action = record.Action.ToString(),
            Category = record.Category.ToString(),
            Actor = record.Actor,
            CorrelationId = record.CorrelationId,
            Timestamp = record.Timestamp,
            Description = record.Description,
            Payload = record.Payload
        };
    }
}
