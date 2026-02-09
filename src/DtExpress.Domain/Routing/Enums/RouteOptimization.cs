namespace DtExpress.Domain.Routing.Enums;

/// <summary>
/// Route optimization objective. Maps 1-to-1 with IRouteStrategy implementations.
/// </summary>
public enum RouteOptimization
{
    /// <summary>最快路线 — Minimize transit time.</summary>
    Fastest,

    /// <summary>最经济 — Minimize shipping cost.</summary>
    Cheapest,

    /// <summary>平衡路线 — Balance time and cost.</summary>
    Balanced
}
