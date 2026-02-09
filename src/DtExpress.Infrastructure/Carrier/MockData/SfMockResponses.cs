using DtExpress.Domain.Carrier.Enums;
using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Infrastructure.Carrier.MockData;

/// <summary>
/// Static mock data simulating SF Express (顺丰速运) API responses.
/// SF is China's premium express carrier — fast, reliable, higher-priced.
/// <para>
/// Response format: JSON-style flat structures with numeric tracking numbers.
/// </para>
/// </summary>
internal static class SfMockResponses
{
    /// <summary>SF tracking number prefix.</summary>
    private const string TrackingPrefix = "SF";

    /// <summary>
    /// Weight-based pricing tiers for SF Express (CNY per kg).
    /// SF charges a premium for guaranteed speed.
    /// </summary>
    private static readonly (decimal MaxKg, decimal PricePerKg)[] PricingTiers =
    [
        (1m,    22.00m),   // ≤1kg  — small parcel premium
        (5m,    18.00m),   // ≤5kg  — standard rate
        (10m,   15.00m),   // ≤10kg — volume discount
        (30m,   12.00m),   // ≤30kg — bulk rate
        (decimal.MaxValue, 10.00m)  // 30kg+ — heavy freight
    ];

    /// <summary>
    /// Service level multipliers applied to base price.
    /// Express is SF's core offering — already priced at premium.
    /// </summary>
    private static readonly Dictionary<ServiceLevel, decimal> ServiceMultipliers = new()
    {
        [ServiceLevel.Express]  = 1.5m,   // 次日达 — next-day guarantee
        [ServiceLevel.Standard] = 1.0m,   // 2-3日达
        [ServiceLevel.Economy]  = 0.8m,   // 3-5日达 — still faster than competitors
    };

    /// <summary>
    /// Estimated delivery days by service level.
    /// SF is known for next-day delivery to major cities.
    /// </summary>
    private static readonly Dictionary<ServiceLevel, int> EstimatedDays = new()
    {
        [ServiceLevel.Express]  = 1,
        [ServiceLevel.Standard] = 2,
        [ServiceLevel.Economy]  = 3,
    };

    /// <summary>
    /// Calculate SF Express quote from a weight and service level.
    /// Simulates SF's tiered pricing model.
    /// </summary>
    internal static (Money Price, int Days) CalculateQuote(Weight weight, ServiceLevel serviceLevel)
    {
        var kg = weight.ToKilograms();
        var pricePerKg = PricingTiers.First(t => kg <= t.MaxKg).PricePerKg;

        // Base price: weight × rate, minimum 22 CNY
        var basePrice = Math.Max(kg * pricePerKg, 22.00m);

        // Apply service multiplier
        var multiplier = ServiceMultipliers.GetValueOrDefault(serviceLevel, 1.0m);
        var finalPrice = Math.Round(basePrice * multiplier, 2);

        var days = EstimatedDays.GetValueOrDefault(serviceLevel, 2);

        return (Money.CNY(finalPrice), days);
    }

    /// <summary>
    /// Generate a 12-digit numeric SF tracking number.
    /// Format: SF + 10 digits (deterministic from booking details).
    /// </summary>
    internal static string GenerateTrackingNumber(string originCity, string destCity, long seed)
    {
        var hash = HashCode.Combine(originCity, destCity, seed);
        var digits = Math.Abs(hash) % 1_000_000_000L;
        return $"{TrackingPrefix}{digits:D10}";
    }

    /// <summary>
    /// Get mock tracking info for a given tracking number.
    /// Simulates SF's JSON-style tracking response:
    /// { "waybillNo": "SF...", "status": "IN_TRANSIT", "location": "上海中转站" }
    /// </summary>
    internal static (ShipmentStatus Status, string? Location) GetTrackingStatus(string trackingNumber)
    {
        // Deterministic status based on tracking number hash
        var hash = Math.Abs(trackingNumber.GetHashCode());
        var statusIndex = hash % 4;

        return statusIndex switch
        {
            0 => (ShipmentStatus.PickedUp, "顺丰上海集散中心"),
            1 => (ShipmentStatus.InTransit, "顺丰武汉中转站"),
            2 => (ShipmentStatus.OutForDelivery, "顺丰同城配送站"),
            _ => (ShipmentStatus.Delivered, "已签收 — 本人签收"),
        };
    }

    /// <summary>
    /// Simulated API response delay for SF Express.
    /// SF has a well-optimized API — faster responses (50-120ms).
    /// </summary>
    internal static TimeSpan GetSimulatedDelay(long seed)
    {
        var ms = 50 + (Math.Abs(seed) % 70); // 50-119ms
        return TimeSpan.FromMilliseconds(ms);
    }
}
