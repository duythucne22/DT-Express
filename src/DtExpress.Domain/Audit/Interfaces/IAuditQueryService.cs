using DtExpress.Domain.Audit.Models;

namespace DtExpress.Domain.Audit.Interfaces;

/// <summary>Read-only queries over the audit stream. Never mutates.</summary>
public interface IAuditQueryService
{
    /// <summary>Get audit trail for a specific entity (timeline view).</summary>
    Task<IReadOnlyList<AuditRecord>> GetByEntityAsync(
        string entityType, string entityId, CancellationToken ct = default);

    /// <summary>Get all audit records sharing a correlation ID (request tracing).</summary>
    Task<IReadOnlyList<AuditRecord>> GetByCorrelationAsync(
        string correlationId, CancellationToken ct = default);
}
