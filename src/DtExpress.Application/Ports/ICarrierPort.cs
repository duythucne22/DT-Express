using DtExpress.Domain.Carrier.Models;

namespace DtExpress.Application.Ports;

/// <summary>Cross-domain port: Orders → Carrier. Used by ShipOrderHandler.</summary>
public interface ICarrierPort
{
    Task<IReadOnlyList<CarrierQuote>> GetQuotesAsync(QuoteRequest request, CancellationToken ct = default);
    Task<BookingResult> BookBestAsync(QuoteRequest request, CancellationToken ct = default);
}
