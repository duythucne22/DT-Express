using System.ComponentModel.DataAnnotations;

namespace DtExpress.Api.Models.Carrier;

// ─────────────────────────────────────────────────────────────────
//  Shared sub-DTOs
// ─────────────────────────────────────────────────────────────────

/// <summary>Postal address for carrier operations.</summary>
public sealed record AddressDto
{
    [Required] public string Street { get; init; } = null!;
    [Required] public string City { get; init; } = null!;
    [Required] public string Province { get; init; } = null!;
    [Required] public string PostalCode { get; init; } = null!;
    public string Country { get; init; } = "CN";
}

/// <summary>Contact information for sender/recipient.</summary>
public sealed record ContactInfoDto
{
    [Required] public string Name { get; init; } = null!;
    [Required] public string Phone { get; init; } = null!;
    public string? Email { get; init; }
}

/// <summary>Package weight with unit.</summary>
public sealed record CarrierWeightDto(
    [Required, Range(0.01, double.MaxValue)] decimal Value,
    [Required] string Unit);

/// <summary>Money amount with currency code.</summary>
public sealed record CarrierMoneyDto(decimal Amount, string Currency);

// ─────────────────────────────────────────────────────────────────
//  GET /api/carriers
// ─────────────────────────────────────────────────────────────────

/// <summary>Registered carrier summary.</summary>
public sealed record CarrierInfoResponse(string CarrierCode, string Name);

// ─────────────────────────────────────────────────────────────────
//  POST /api/carriers/quotes
// ─────────────────────────────────────────────────────────────────

/// <summary>Request body for getting carrier quotes.</summary>
public sealed record GetQuotesRequest
{
    [Required] public AddressDto Origin { get; init; } = null!;
    [Required] public AddressDto Destination { get; init; } = null!;
    [Required] public CarrierWeightDto Weight { get; init; } = null!;
    [Required] public string ServiceLevel { get; init; } = null!;
}

/// <summary>Single carrier quote.</summary>
public sealed record CarrierQuoteResponse(
    string CarrierCode,
    CarrierMoneyDto Price,
    int EstimatedDays,
    string ServiceLevel);

/// <summary>All quotes plus recommendation.</summary>
public sealed record QuotesResponse
{
    public IReadOnlyList<CarrierQuoteResponse> Quotes { get; init; } = [];
    public RecommendedCarrier? Recommended { get; init; }
}

/// <summary>Recommended carrier with selection reason.</summary>
public sealed record RecommendedCarrier(string CarrierCode, string Reason);

// ─────────────────────────────────────────────────────────────────
//  POST /api/carriers/{code}/book
// ─────────────────────────────────────────────────────────────────

/// <summary>Request body for booking a shipment with a specific carrier.</summary>
public sealed record BookShipmentRequest
{
    [Required] public AddressDto Origin { get; init; } = null!;
    [Required] public AddressDto Destination { get; init; } = null!;
    [Required] public CarrierWeightDto Weight { get; init; } = null!;
    [Required] public ContactInfoDto Sender { get; init; } = null!;
    [Required] public ContactInfoDto Recipient { get; init; } = null!;
    [Required] public string ServiceLevel { get; init; } = null!;
}

/// <summary>Booking confirmation.</summary>
public sealed record BookingResponse(
    string CarrierCode,
    string TrackingNumber,
    DateTimeOffset BookedAt);

// ─────────────────────────────────────────────────────────────────
//  GET /api/carriers/{code}/track/{trackingNo}
// ─────────────────────────────────────────────────────────────────

/// <summary>Carrier tracking info response.</summary>
public sealed record CarrierTrackingResponse(
    string TrackingNumber,
    string Status,
    string? CurrentLocation,
    DateTimeOffset UpdatedAt);
