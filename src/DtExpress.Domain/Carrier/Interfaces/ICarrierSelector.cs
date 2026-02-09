using DtExpress.Domain.Carrier.Models;

namespace DtExpress.Domain.Carrier.Interfaces;

/// <summary>
/// Strategy Pattern: selects the best quote from a collection.
/// Different policies: cheapest, fastest, etc.
/// </summary>
public interface ICarrierSelector
{
    /// <summary>Select the best quote based on this selector's policy.</summary>
    CarrierQuote SelectBest(IEnumerable<CarrierQuote> quotes);
}
