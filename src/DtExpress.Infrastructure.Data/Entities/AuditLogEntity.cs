namespace DtExpress.Infrastructure.Data.Entities;

/// <summary>
/// EF entity mapping to the <c>audit_logs</c> table.
/// Immutable — append-only, never modified after creation.
/// </summary>
public sealed class AuditLogEntity
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;

    public string Action { get; set; } = null!;   // Created, Updated, Deleted, StateChanged, BusinessAction
    public string Category { get; set; } = null!;  // DataChange, StateTransition, ExternalCall, BusinessDecision

    // ── Actor: dual reference pattern ───────────────────────────
    public Guid? ActorUserId { get; set; }
    public string ActorName { get; set; } = null!;

    public string CorrelationId { get; set; } = null!;

    public DateTimeOffset Timestamp { get; set; }
    public string? Description { get; set; }
    public string? Payload { get; set; } // JSONB — serialized Dictionary<string, object?>

    public DateTimeOffset CreatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────────
    public UserEntity? ActorUser { get; set; }
}
