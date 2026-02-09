using DtExpress.Domain.Carrier.Models;

namespace DtExpress.Domain.Carrier.Interfaces;

/// <summary>
/// Adapter Pattern: normalizes heterogeneous carrier APIs into unified operations.
/// Each implementation maps a specific carrier's data format (JSON/XML) to domain models.
/// </summary>
public interface ICarrierAdapter
{
    /// <summary>Carrier identifier, e.g. "SF", "JD".</summary>
    string CarrierCode { get; }

    /// <summary>Get a shipping quote from this carrier.</summary>
    Task<CarrierQuote> GetQuoteAsync(QuoteRequest request, CancellationToken ct = default);

    /// <summary>Book a shipment with this carrier.</summary>
    Task<BookingResult> BookAsync(BookingRequest request, CancellationToken ct = default);

    /// <summary>Get tracking info for a shipment.</summary>
    Task<CarrierTrackingInfo> TrackAsync(string trackingNumber, CancellationToken ct = default);
}
