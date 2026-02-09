using DtExpress.Domain.Audit.Interfaces;
using DtExpress.Domain.Audit.Models;

namespace DtExpress.Infrastructure.Audit;

/// <summary>
/// In-memory append-only audit store. Implements <b>both</b> write and read interfaces:
/// <list type="bullet">
///   <item><see cref="IAuditSink"/>  — append-only write (decorator target for PII masking)</item>
///   <item><see cref="IAuditQueryService"/> — read-only LINQ queries over the stored records</item>
/// </list>
/// DI wiring resolves <see cref="IAuditQueryService"/> to the same singleton
/// instance so reads hit the same <c>List&lt;AuditRecord&gt;</c>.
/// Thread-safe via <c>lock</c> on the backing list.
/// </summary>
public sealed class InMemoryAuditSink : IAuditSink, IAuditQueryService
{
    private readonly List<AuditRecord> _records = [];
    private readonly object _lock = new();

    // ── IAuditSink ──────────────────────────────────────────────

    /// <inheritdoc />
    public Task AppendAsync(AuditRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_lock)
        {
            _records.Add(record);
        }

        return Task.CompletedTask;
    }

    // ── IAuditQueryService ──────────────────────────────────────

    /// <inheritdoc />
    public Task<IReadOnlyList<AuditRecord>> GetByEntityAsync(
        string entityType, string entityId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        IReadOnlyList<AuditRecord> result;
        lock (_lock)
        {
            result = _records
                .Where(r => r.EntityType == entityType && r.EntityId == entityId)
                .OrderBy(r => r.Timestamp)
                .ToList();
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AuditRecord>> GetByCorrelationAsync(
        string correlationId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        IReadOnlyList<AuditRecord> result;
        lock (_lock)
        {
            result = _records
                .Where(r => r.CorrelationId == correlationId)
                .OrderBy(r => r.Timestamp)
                .ToList();
        }

        return Task.FromResult(result);
    }
}
