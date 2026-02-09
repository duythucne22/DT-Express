using DtExpress.Domain.Audit.Enums;

namespace DtExpress.Domain.Audit.Models;

/// <summary>
/// Immutable audit record — append-only, never modified after creation.
/// Captures who did what, to which entity, when, and with what payload.
/// </summary>
public sealed record AuditRecord(
    string Id,
    string EntityType,
    string EntityId,
    AuditAction Action,
    AuditCategory Category,
    string Actor,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string? Description,
    Dictionary<string, object?>? Payload);
