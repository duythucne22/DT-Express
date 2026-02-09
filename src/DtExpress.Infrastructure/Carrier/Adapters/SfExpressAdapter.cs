using DtExpress.Domain.Carrier.Enums;
using DtExpress.Domain.Carrier.Interfaces;
using DtExpress.Domain.Carrier.Models;
using DtExpress.Infrastructure.Carrier.MockData;

namespace DtExpress.Infrastructure.Carrier.Adapters;

/// <summary>
/// Adapter Pattern implementation for SF Express (顺丰速运).
/// Normalizes SF's JSON-style API responses into unified domain models.
/// <para>
/// SF Express is China's premium courier — known for:
/// <list type="bullet">
///   <item>Next-day delivery (次日达) for 300+ cities</item>
///   <item>12-digit numeric tracking numbers (SF + 10 digits)</item>
///   <item>Premium pricing with guaranteed time windows</item>
///   <item>JSON flat response format</item>
/// </list>
/// </para>
/// </summary>
public sealed class SfExpressAdapter : ICarrierAdapter
{
    /// <inheritdoc />
    public string CarrierCode => CarrierCode_SF;

    /// <summary>Carrier code constant for DI resolution.</summary>
    private const string CarrierCode_SF = "SF";

    /// <summary>Monotonic counter for deterministic tracking number generation.</summary>
    private long _bookingCounter;

    /// <inheritdoc />
    /// <remarks>
    /// Simulates SF's quote API:
    /// POST /api/quote → { "waybillNo": null, "serviceType": "EXPRESS", "totalFee": 33.00 }
    /// Maps flat JSON response into <see cref="CarrierQuote"/>.
    /// </remarks>
    public async Task<CarrierQuote> GetQuoteAsync(QuoteRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Simulate SF's fast JSON API response time (50-120ms)
        var delay = SfMockResponses.GetSimulatedDelay(
            HashCode.Combine(request.Origin.City, request.Destination.City));
        await Task.Delay(delay, ct);

        var (price, days) = SfMockResponses.CalculateQuote(request.Weight, request.ServiceLevel);

        return new CarrierQuote(
            CarrierCode: CarrierCode_SF,
            Price: price,
            EstimatedDays: days,
            ServiceLevel: request.ServiceLevel);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Simulates SF's booking API:
    /// POST /api/order → { "waybillNo": "SF1234567890", "success": true, "createTime": "..." }
    /// Returns 12-digit numeric tracking number.
    /// </remarks>
    public async Task<BookingResult> BookAsync(BookingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Simulate API call
        var counter = Interlocked.Increment(ref _bookingCounter);
        var delay = SfMockResponses.GetSimulatedDelay(counter);
        await Task.Delay(delay, ct);

        var trackingNumber = SfMockResponses.GenerateTrackingNumber(
            request.Origin.City,
            request.Destination.City,
            counter);

        return new BookingResult(
            CarrierCode: CarrierCode_SF,
            TrackingNumber: trackingNumber,
            BookedAt: DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Simulates SF's tracking API:
    /// GET /api/track?waybillNo=SF... → { "waybillNo": "SF...", "status": "DELIVERING", "opNode": "顺丰同城配送站" }
    /// Maps SF's status codes (COLLECTED → TRANSPORTING → DELIVERING → SIGNED) to domain <see cref="ShipmentStatus"/>.
    /// </remarks>
    public async Task<CarrierTrackingInfo> TrackAsync(string trackingNumber, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        // Simulate API call
        var delay = SfMockResponses.GetSimulatedDelay(trackingNumber.GetHashCode());
        await Task.Delay(delay, ct);

        var (status, location) = SfMockResponses.GetTrackingStatus(trackingNumber);

        return new CarrierTrackingInfo(
            TrackingNumber: trackingNumber,
            Status: status,
            CurrentLocation: location,
            UpdatedAt: DateTimeOffset.UtcNow);
    }
}
