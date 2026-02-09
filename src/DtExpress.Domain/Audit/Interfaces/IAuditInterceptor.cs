using DtExpress.Domain.Audit.Models;

namespace DtExpress.Domain.Audit.Interfaces;

/// <summary>
/// Interceptor Pattern: captures changes from a context and produces audit records.
/// Called at domain boundaries (state transitions, external calls).
/// </summary>
public interface IAuditInterceptor
{
    /// <summary>Capture changes from the given context, return audit records to append.</summary>
    IReadOnlyList<AuditRecord> CaptureChanges(AuditContext context);
}
