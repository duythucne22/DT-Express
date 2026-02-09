using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Carrier.Models;

/// <summary>
/// Input contract for booking a shipment with a specific carrier.
/// </summary>
public sealed record BookingRequest(
    string CarrierCode,
    Address Origin,
    Address Destination,
    Weight Weight,
    ContactInfo Sender,
    ContactInfo Recipient,
    ServiceLevel ServiceLevel);
