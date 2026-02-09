using DtExpress.Domain.Audit.Enums;

namespace DtExpress.Domain.Audit.Models;

/// <summary>
/// Input context for the audit interceptor — describes what changed.
/// Before/After dictionaries enable diff-based audit trails.
/// </summary>
public sealed record AuditContext(
    string EntityType,
    string EntityId,
    AuditAction Action,
    AuditCategory Category,
    string? Description = null,
    Dictionary<string, object?>? Before = null,
    Dictionary<string, object?>? After = null);
