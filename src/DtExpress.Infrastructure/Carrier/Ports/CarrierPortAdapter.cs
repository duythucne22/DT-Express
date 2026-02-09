using DtExpress.Application.Ports;
using DtExpress.Domain.Carrier.Interfaces;
using DtExpress.Domain.Carrier.Models;

namespace DtExpress.Infrastructure.Carrier.Ports;

/// <summary>
/// Port Adapter: bridges <see cref="ICarrierPort"/> (Application layer) to
/// Infrastructure carrier implementations via <see cref="ICarrierAdapterFactory"/>
/// and <see cref="ICarrierSelector"/>.
/// <para>
/// Used by <c>ShipOrderHandler</c> to get quotes and book the best carrier
/// without knowing about specific carrier adapters.
/// </para>
/// </summary>
public sealed class CarrierPortAdapter : ICarrierPort
{
    private readonly ICarrierAdapterFactory _adapterFactory;
    private readonly ICarrierSelector _selector;

    public CarrierPortAdapter(
        ICarrierAdapterFactory adapterFactory,
        ICarrierSelector selector)
    {
        _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Calls <see cref="ICarrierAdapter.GetQuoteAsync"/> on every registered adapter
    /// (resolved via <see cref="ICarrierAdapterFactory.GetAll"/>), collecting all quotes.
    /// </remarks>
    public async Task<IReadOnlyList<CarrierQuote>> GetQuotesAsync(
        QuoteRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var adapters = _adapterFactory.GetAll();
        var quotes = new List<CarrierQuote>(adapters.Count);

        foreach (var adapter in adapters)
        {
            var quote = await adapter.GetQuoteAsync(request, ct);
            quotes.Add(quote);
        }

        return quotes.AsReadOnly();
    }

    /// <inheritdoc />
    /// <remarks>
    /// 1. Gets quotes from all carriers via <see cref="GetQuotesAsync"/>.
    /// 2. Selects the best quote using the injected <see cref="ICarrierSelector"/> policy.
    /// 3. Books with the selected carrier via <see cref="ICarrierAdapterFactory.Resolve"/>.
    /// </remarks>
    public async Task<BookingResult> BookBestAsync(
        QuoteRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Get quotes from all carriers
        var quotes = await GetQuotesAsync(request, ct);

        // 2. Select best quote using the configured selector policy
        var bestQuote = _selector.SelectBest(quotes);

        // 3. Resolve the winning carrier's adapter and book
        var adapter = _adapterFactory.Resolve(bestQuote.CarrierCode);

        var bookingRequest = new BookingRequest(
            CarrierCode: bestQuote.CarrierCode,
            Origin: request.Origin,
            Destination: request.Destination,
            Weight: request.Weight,
            Sender: new DtExpress.Domain.ValueObjects.ContactInfo("发件人", "13800000000"),
            Recipient: new DtExpress.Domain.ValueObjects.ContactInfo("收件人", "13900000000"),
            ServiceLevel: request.ServiceLevel);

        return await adapter.BookAsync(bookingRequest, ct);
    }
}
