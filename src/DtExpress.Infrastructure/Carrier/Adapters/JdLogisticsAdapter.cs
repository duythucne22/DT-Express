using DtExpress.Domain.Carrier.Enums;
using DtExpress.Domain.Carrier.Interfaces;
using DtExpress.Domain.Carrier.Models;
using DtExpress.Infrastructure.Carrier.MockData;

namespace DtExpress.Infrastructure.Carrier.Adapters;

/// <summary>
/// Adapter Pattern implementation for JD Logistics (京东物流).
/// Normalizes JD's XML-style API responses into unified domain models.
/// <para>
/// JD Logistics is China's e-commerce-focused carrier — known for:
/// <list type="bullet">
///   <item>Competitive pricing for standard/economy tiers</item>
///   <item>10-15 character alphanumeric tracking numbers (JD + alphanumeric suffix)</item>
///   <item>Extensive warehouse network (京东仓配一体)</item>
///   <item>XML nested response format</item>
/// </list>
/// </para>
/// </summary>
public sealed class JdLogisticsAdapter : ICarrierAdapter
{
    /// <inheritdoc />
    public string CarrierCode => CarrierCode_JD;

    /// <summary>Carrier code constant for DI resolution.</summary>
    private const string CarrierCode_JD = "JD";

    /// <summary>Monotonic counter for deterministic tracking number generation.</summary>
    private long _bookingCounter;

    /// <inheritdoc />
    /// <remarks>
    /// Simulates JD's quote API:
    /// POST /api/v2/freight/query →
    /// <![CDATA[<response><code>0</code><data><freight>12.00</freight><deliveryDays>3</deliveryDays></data></response>]]>
    /// Parses nested XML into <see cref="CarrierQuote"/>.
    /// </remarks>
    public async Task<CarrierQuote> GetQuoteAsync(QuoteRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Simulate JD's slower XML API response time (100-200ms)
        var delay = JdMockResponses.GetSimulatedDelay(
            HashCode.Combine(request.Origin.City, request.Destination.City));
        await Task.Delay(delay, ct);

        var (price, days) = JdMockResponses.CalculateQuote(request.Weight, request.ServiceLevel);

        return new CarrierQuote(
            CarrierCode: CarrierCode_JD,
            Price: price,
            EstimatedDays: days,
            ServiceLevel: request.ServiceLevel);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Simulates JD's booking API:
    /// POST /api/v2/order/create →
    /// <![CDATA[<response><code>0</code><data><waybillCode>JDA1B2C3D4E5</waybillCode>
    /// <createTime>2025-01-15T10:30:00Z</createTime></data></response>]]>
    /// Returns 10-15 character alphanumeric tracking number.
    /// </remarks>
    public async Task<BookingResult> BookAsync(BookingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Simulate API call
        var counter = Interlocked.Increment(ref _bookingCounter);
        var delay = JdMockResponses.GetSimulatedDelay(counter);
        await Task.Delay(delay, ct);

        var trackingNumber = JdMockResponses.GenerateTrackingNumber(
            request.Origin.City,
            request.Destination.City,
            counter);

        return new BookingResult(
            CarrierCode: CarrierCode_JD,
            TrackingNumber: trackingNumber,
            BookedAt: DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Simulates JD's tracking API:
    /// GET /api/v2/trace/query?waybillCode=JD... →
    /// <![CDATA[<response><code>0</code><data><state>TRANSPORTING</state>
    /// <operationSite>京东武汉转运中心</operationSite></data></response>]]>
    /// Maps JD's state codes (WAIT_PICKUP → TRANSPORTING → DELIVERING → SIGNED) to domain <see cref="ShipmentStatus"/>.
    /// </remarks>
    public async Task<CarrierTrackingInfo> TrackAsync(string trackingNumber, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        // Simulate API call
        var delay = JdMockResponses.GetSimulatedDelay(trackingNumber.GetHashCode());
        await Task.Delay(delay, ct);

        var (status, location) = JdMockResponses.GetTrackingStatus(trackingNumber);

        return new CarrierTrackingInfo(
            TrackingNumber: trackingNumber,
            Status: status,
            CurrentLocation: location,
            UpdatedAt: DateTimeOffset.UtcNow);
    }
}
