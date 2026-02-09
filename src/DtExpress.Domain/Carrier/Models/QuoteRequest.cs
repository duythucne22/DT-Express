using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Carrier.Models;

/// <summary>
/// Input contract for requesting a shipping quote from a carrier.
/// </summary>
public sealed record QuoteRequest(
    Address Origin,
    Address Destination,
    Weight Weight,
    ServiceLevel ServiceLevel);
