using DtExpress.Domain.Carrier.Interfaces;
using DtExpress.Domain.Carrier.Models;

namespace DtExpress.Infrastructure.Carrier.Selectors;

/// <summary>
/// Strategy Pattern: selects the carrier quote with the fewest estimated delivery days.
/// Used when delivery speed is the primary selection criterion.
/// </summary>
public sealed class FastestCarrierSelector : ICarrierSelector
{
    /// <inheritdoc />
    /// <remarks>
    /// Compares <see cref="CarrierQuote.EstimatedDays"/>.
    /// If tied on days, breaks tie by choosing the cheaper option.
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

        var fastest = quoteList[0];
        for (var i = 1; i < quoteList.Count; i++)
        {
            var candidate = quoteList[i];

            // Primary criterion: fewest estimated days
            if (candidate.EstimatedDays < fastest.EstimatedDays)
            {
                fastest = candidate;
            }
            // Tie-breaker: if same days, prefer the cheaper option
            else if (candidate.EstimatedDays == fastest.EstimatedDays
                     && candidate.Price.Amount < fastest.Price.Amount)
            {
                fastest = candidate;
            }
        }

        return fastest;
    }
}
