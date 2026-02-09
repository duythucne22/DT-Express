namespace DtExpress.Domain.Carrier.Enums;

/// <summary>
/// Well-known carrier code constants.
/// String constants (not enum) to satisfy Open/Closed Principle —
/// adding a new carrier requires only a new const, no recompilation of consumers.
/// </summary>
public static class CarrierCode
{
    /// <summary>顺丰速运 — SF Express. Tracking: 12-digit numeric.</summary>
    public const string SfExpress = "SF";

    /// <summary>京东物流 — JD Logistics. Tracking: 10-15 alphanumeric.</summary>
    public const string JdLogistics = "JD";
}
