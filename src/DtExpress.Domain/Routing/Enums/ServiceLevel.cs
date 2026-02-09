namespace DtExpress.Domain.Routing.Enums;

/// <summary>
/// Delivery speed tier. Shared across Routing, Carrier, and Orders domains.
/// </summary>
public enum ServiceLevel
{
    /// <summary>次日达 — Next-day delivery.</summary>
    Express,

    /// <summary>2-3日达 — Standard 2–3 day delivery.</summary>
    Standard,

    /// <summary>3-5日达 — Economy 3–5 day delivery.</summary>
    Economy
}
