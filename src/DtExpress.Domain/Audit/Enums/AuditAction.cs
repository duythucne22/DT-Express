namespace DtExpress.Domain.Audit.Enums;

/// <summary>
/// The type of change captured by an audit record.
/// </summary>
public enum AuditAction
{
    /// <summary>新建 — Entity created.</summary>
    Created,

    /// <summary>更新 — Entity field(s) updated.</summary>
    Updated,

    /// <summary>删除 — Entity deleted.</summary>
    Deleted,

    /// <summary>状态变更 — State machine transition (e.g. Order status change).</summary>
    StateChanged,

    /// <summary>业务操作 — Business action (e.g. carrier booking, route calculation).</summary>
    BusinessAction
}
