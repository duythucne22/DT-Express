namespace DtExpress.Api.Models.Audit;

// ─────────────────────────────────────────────────────────────────
//  GET /api/audit/entity/{entityType}/{entityId}
//  GET /api/audit/correlation/{correlationId}
// ─────────────────────────────────────────────────────────────────

/// <summary>Audit record response DTO.</summary>
public sealed record AuditRecordResponse
{
    /// <summary>Unique audit record ID.</summary>
    public string Id { get; init; } = null!;

    /// <summary>Type of entity audited (e.g. "Order", "Route").</summary>
    public string EntityType { get; init; } = null!;

    /// <summary>ID of the entity.</summary>
    public string EntityId { get; init; } = null!;

    /// <summary>Action performed (Created, Updated, StateChanged, etc.).</summary>
    public string Action { get; init; } = null!;

    /// <summary>Category of the audit event.</summary>
    public string Category { get; init; } = null!;

    /// <summary>Who performed the action.</summary>
    public string Actor { get; init; } = null!;

    /// <summary>Correlation ID linking related audit records.</summary>
    public string CorrelationId { get; init; } = null!;

    /// <summary>When the action occurred.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Human-readable description of what happened.</summary>
    public string? Description { get; init; }

    /// <summary>Additional structured data (before/after values, etc.).</summary>
    public Dictionary<string, object?>? Payload { get; init; }
}
