using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Carrier.Models;

/// <summary>
/// A carrier's response to a quote request — price and estimated delivery time.
/// </summary>
public sealed record CarrierQuote(
    string CarrierCode,
    Money Price,
    int EstimatedDays,
    ServiceLevel ServiceLevel);
