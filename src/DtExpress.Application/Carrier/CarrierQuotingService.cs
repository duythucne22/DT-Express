using DtExpress.Domain.Carrier.Interfaces;
using DtExpress.Domain.Carrier.Models;

namespace DtExpress.Application.Carrier;

/// <summary>
/// Application service: gets quotes from all registered carrier adapters
/// and selects the best using the injected ICarrierSelector policy.
/// </summary>
public sealed class CarrierQuotingService
{
    private readonly ICarrierAdapterFactory _adapterFactory;
    private readonly ICarrierSelector _selector;

    public CarrierQuotingService(
        ICarrierAdapterFactory adapterFactory,
        ICarrierSelector selector)
    {
        _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    /// <summary>Get quotes from all registered carriers.</summary>
    public async Task<IReadOnlyList<CarrierQuote>> GetQuotesAsync(
        QuoteRequest request, CancellationToken ct = default)
    {
        var adapters = _adapterFactory.GetAll();
        var quotes = new List<CarrierQuote>();

        foreach (var adapter in adapters)
        {
            var quote = await adapter.GetQuoteAsync(request, ct);
            quotes.Add(quote);
        }

        return quotes.AsReadOnly();
    }

    /// <summary>Get quotes from all carriers, then select the best one.</summary>
    public async Task<CarrierQuote> GetBestQuoteAsync(
        QuoteRequest request, CancellationToken ct = default)
    {
        var quotes = await GetQuotesAsync(request, ct);
        return _selector.SelectBest(quotes);
    }
}
