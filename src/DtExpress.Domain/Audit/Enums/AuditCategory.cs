namespace DtExpress.Domain.Audit.Enums;

/// <summary>
/// Classification of the audit record for filtering and reporting.
/// </summary>
public enum AuditCategory
{
    /// <summary>数据变更 — Direct data mutation (CRUD).</summary>
    DataChange,

    /// <summary>状态流转 — State machine transition.</summary>
    StateTransition,

    /// <summary>外部调用 — External service call (carrier API, map service).</summary>
    ExternalCall,

    /// <summary>业务决策 — Business logic decision (route selection, carrier selection).</summary>
    BusinessDecision
}
