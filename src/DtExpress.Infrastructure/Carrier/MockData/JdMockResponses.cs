using DtExpress.Domain.Carrier.Enums;
using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Infrastructure.Carrier.MockData;

/// <summary>
/// Static mock data simulating JD Logistics (京东物流) API responses.
/// JD is an e-commerce-focused logistics provider — competitive pricing, good coverage.
/// <para>
/// Response format: XML-style nested structures with alphanumeric tracking numbers.
/// </para>
/// </summary>
internal static class JdMockResponses
{
    /// <summary>JD tracking number prefix.</summary>
    private const string TrackingPrefix = "JD";

    /// <summary>
    /// Weight-based pricing tiers for JD Logistics (CNY per kg).
    /// JD offers competitive e-commerce shipping rates.
    /// </summary>
    private static readonly (decimal MaxKg, decimal PricePerKg)[] PricingTiers =
    [
        (1m,    15.00m),   // ≤1kg  — small parcel
        (5m,    12.00m),   // ≤5kg  — standard rate
        (10m,   10.00m),   // ≤10kg — volume discount
        (30m,    8.00m),   // ≤30kg — bulk rate
        (decimal.MaxValue,  6.50m)  // 30kg+ — heavy freight
    ];

    /// <summary>
    /// Service level multipliers applied to base price.
    /// JD's economy tier is their strength — high volume, low cost.
    /// </summary>
    private static readonly Dictionary<ServiceLevel, decimal> ServiceMultipliers = new()
    {
        [ServiceLevel.Express]  = 1.8m,   // 次日达 — premium surcharge (not JD's strength)
        [ServiceLevel.Standard] = 1.0m,   // 2-3日达 — JD's sweet spot
        [ServiceLevel.Economy]  = 0.7m,   // 3-5日达 — JD's core offering
    };

    /// <summary>
    /// Estimated delivery days by service level.
    /// JD is 1-2 days slower than SF for express, but competitive on standard/economy.
    /// </summary>
    private static readonly Dictionary<ServiceLevel, int> EstimatedDays = new()
    {
        [ServiceLevel.Express]  = 2,
        [ServiceLevel.Standard] = 3,
        [ServiceLevel.Economy]  = 5,
    };

    /// <summary>
    /// Calculate JD Logistics quote from a weight and service level.
    /// Simulates JD's competitive pricing model.
    /// </summary>
    internal static (Money Price, int Days) CalculateQuote(Weight weight, ServiceLevel serviceLevel)
    {
        var kg = weight.ToKilograms();
        var pricePerKg = PricingTiers.First(t => kg <= t.MaxKg).PricePerKg;

        // Base price: weight × rate, minimum 8 CNY (JD's lower minimum)
        var basePrice = Math.Max(kg * pricePerKg, 8.00m);

        // Apply service multiplier
        var multiplier = ServiceMultipliers.GetValueOrDefault(serviceLevel, 1.0m);
        var finalPrice = Math.Round(basePrice * multiplier, 2);

        var days = EstimatedDays.GetValueOrDefault(serviceLevel, 3);

        return (Money.CNY(finalPrice), days);
    }

    /// <summary>
    /// Generate a 10-15 character alphanumeric JD tracking number.
    /// Format: JD + 8-13 alphanumeric characters (deterministic from booking details).
    /// Simulates JD's XML-style <trackingCode>JDA1B2C3D4E5</trackingCode> response.
    /// </summary>
    internal static string GenerateTrackingNumber(string originCity, string destCity, long seed)
    {
        var hash = Math.Abs(HashCode.Combine(originCity, destCity, seed));

        // Build alphanumeric suffix (8-13 chars)
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var length = 8 + (hash % 6); // 8-13 chars
        var rng = new Random(hash);

        Span<char> buffer = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = chars[rng.Next(chars.Length)];
        }

        return $"{TrackingPrefix}{new string(buffer)}";
    }

    /// <summary>
    /// Get mock tracking info for a given tracking number.
    /// Simulates JD's XML-style tracking response:
    /// <![CDATA[<response><waybillCode>JD...</waybillCode><state>TRANSPORTING</state>
    /// <operationSite>京东北京分拣中心</operationSite></response>]]>
    /// </summary>
    internal static (ShipmentStatus Status, string? Location) GetTrackingStatus(string trackingNumber)
    {
        // Deterministic status based on tracking number hash
        var hash = Math.Abs(trackingNumber.GetHashCode());
        var statusIndex = hash % 5;

        return statusIndex switch
        {
            0 => (ShipmentStatus.Created, "京东上海仓"),
            1 => (ShipmentStatus.PickedUp, "京东北京分拣中心"),
            2 => (ShipmentStatus.InTransit, "京东武汉转运中心"),
            3 => (ShipmentStatus.OutForDelivery, "京东同城配送站"),
            _ => (ShipmentStatus.Delivered, "已签收 — 代收点签收"),
        };
    }

    /// <summary>
    /// Simulated API response delay for JD Logistics.
    /// JD's API is XML-based and slightly slower (100-200ms).
    /// </summary>
    internal static TimeSpan GetSimulatedDelay(long seed)
    {
        var ms = 100 + (Math.Abs(seed) % 100); // 100-199ms
        return TimeSpan.FromMilliseconds(ms);
    }
}
