namespace DtExpress.Domain.Carrier.Models;

/// <summary>
/// Confirmation returned after a successful carrier booking.
/// </summary>
public sealed record BookingResult(
    string CarrierCode,
    string TrackingNumber,
    DateTimeOffset BookedAt);
