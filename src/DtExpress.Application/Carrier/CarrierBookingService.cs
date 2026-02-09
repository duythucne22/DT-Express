using DtExpress.Domain.Carrier.Interfaces;
using DtExpress.Domain.Carrier.Models;

namespace DtExpress.Application.Carrier;

/// <summary>
/// Application service: books a shipment with a specific carrier
/// by resolving the adapter from the factory.
/// </summary>
public sealed class CarrierBookingService
{
    private readonly ICarrierAdapterFactory _adapterFactory;

    public CarrierBookingService(ICarrierAdapterFactory adapterFactory)
    {
        _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
    }

    /// <summary>Book a shipment with the carrier specified in the request.</summary>
    public async Task<BookingResult> BookAsync(
        BookingRequest request, CancellationToken ct = default)
    {
        var adapter = _adapterFactory.Resolve(request.CarrierCode);
        return await adapter.BookAsync(request, ct);
    }
}
