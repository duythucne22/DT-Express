using DtExpress.Domain.Audit.Models;

namespace DtExpress.Domain.Audit.Interfaces;

/// <summary>
/// Append-only audit record storage.
/// Decorator target: PII masking wraps this interface.
/// </summary>
public interface IAuditSink
{
    /// <summary>Append a single audit record. Records are immutable once written.</summary>
    Task AppendAsync(AuditRecord record, CancellationToken ct = default);
}
