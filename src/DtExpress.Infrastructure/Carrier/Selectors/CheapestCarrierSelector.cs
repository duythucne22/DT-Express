using DtExpress.Domain.Carrier.Interfaces;
using DtExpress.Domain.Carrier.Models;

namespace DtExpress.Infrastructure.Carrier.Selectors;

/// <summary>
/// Strategy Pattern: selects the carrier quote with the lowest price.
/// Used when cost optimization is the primary selection criterion.
/// </summary>
public sealed class CheapestCarrierSelector : ICarrierSelector
{
    /// <inheritdoc />
    /// <remarks>
    /// Compares <see cref="CarrierQuote.Price"/> amounts.
    /// Throws if the quote collection is empty.
    /// </remarks>
    public CarrierQuote SelectBest(IEnumerable<CarrierQuote> quotes)
    {
        ArgumentNullException.ThrowIfNull(quotes);

        var quoteList = quotes as IList<CarrierQuote> ?? quotes.ToList();

        if (quoteList.Count == 0)
        {
            throw new InvalidOperationException("Cannot select from an empty collection of quotes.");
        }

        var cheapest = quoteList[0];
        for (var i = 1; i < quoteList.Count; i++)
        {
            if (quoteList[i].Price.Amount < cheapest.Price.Amount)
            {
                cheapest = quoteList[i];
            }
        }

        return cheapest;
    }
}
