using DtExpress.Domain.Audit.Models;

namespace DtExpress.Application.Ports;

/// <summary>Cross-domain port: All handlers → Audit. Records state changes and actions.</summary>
public interface IAuditPort
{
    Task RecordAsync(AuditContext context, CancellationToken ct = default);
}
