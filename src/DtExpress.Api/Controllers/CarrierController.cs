using DtExpress.Api.Models;
using DtExpress.Api.Models.Carrier;
using DtExpress.Application.Carrier;
using DtExpress.Domain.Carrier.Interfaces;
using DtExpress.Domain.Common;
using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace DtExpress.Api.Controllers;

/// <summary>
/// Multi-carrier management: list carriers, get quotes, book shipments, track packages.
/// Delegates to CarrierQuotingService and CarrierBookingService (Application layer).
/// </summary>
[ApiController]
[Route("api/carriers")]
[Produces("application/json")]
[Tags("Carriers")]
public sealed class CarrierController : ControllerBase
{
    private readonly CarrierQuotingService _quotingService;
    private readonly CarrierBookingService _bookingService;
    private readonly ICarrierAdapterFactory _adapterFactory;
    private readonly ICorrelationIdProvider _correlationId;

    public CarrierController(
        CarrierQuotingService quotingService,
        CarrierBookingService bookingService,
        ICarrierAdapterFactory adapterFactory,
        ICorrelationIdProvider correlationId)
    {
        _quotingService = quotingService;
        _bookingService = bookingService;
        _adapterFactory = adapterFactory;
        _correlationId = correlationId;
    }

    /// <summary>List all registered carrier adapters.</summary>
    /// <remarks>Returns carrier codes and display names for SF Express (顺丰) and JD Logistics (京东).</remarks>
    /// <response code="200">List of registered carriers.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CarrierInfoResponse>>), StatusCodes.Status200OK)]
    public IActionResult GetCarriers()
    {
        var adapters = _adapterFactory.GetAll();
        var carriers = adapters.Select(a => new CarrierInfoResponse(
            a.CarrierCode,
            a.CarrierCode switch
            {
                "SF" => "SF Express (顺丰速运)",
                "JD" => "JD Logistics (京东物流)",
                _ => a.CarrierCode
            }
        )).ToList();

        return Ok(ApiResponse<IReadOnlyList<CarrierInfoResponse>>.Ok(carriers, _correlationId.GetCorrelationId()));
    }

    /// <summary>Get shipping quotes from all registered carriers.</summary>
    /// <remarks>
    /// Queries all carriers (SF, JD) for pricing and estimated delivery days.
    /// Also returns a recommendation based on the cheapest option.
    /// </remarks>
    /// <response code="200">Quotes from all carriers with recommendation.</response>
    /// <response code="400">Invalid request data.</response>
    [HttpPost("quotes")]
    [ProducesResponseType(typeof(ApiResponse<QuotesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetQuotes([FromBody] GetQuotesRequest request, CancellationToken ct)
    {
        var quoteRequest = MapToQuoteRequest(request);

        var quotes = await _quotingService.GetQuotesAsync(quoteRequest, ct);
        var bestQuote = await _quotingService.GetBestQuoteAsync(quoteRequest, ct);

        var quoteResponses = quotes.Select(q => new CarrierQuoteResponse(
            q.CarrierCode,
            new CarrierMoneyDto(q.Price.Amount, q.Price.Currency),
            q.EstimatedDays,
            q.ServiceLevel.ToString()
        )).ToList();

        var response = new QuotesResponse
        {
            Quotes = quoteResponses,
            Recommended = new RecommendedCarrier(bestQuote.CarrierCode, "Cheapest")
        };

        return Ok(ApiResponse<QuotesResponse>.Ok(response, _correlationId.GetCorrelationId()));
    }

    /// <summary>Book a shipment with a specific carrier.</summary>
    /// <remarks>
    /// Resolves the carrier adapter by code (case-insensitive), creates a booking,
    /// and returns a tracking number for package tracking.
    /// </remarks>
    /// <param name="code">Carrier code (e.g. "SF" or "JD").</param>
    /// <param name="request">Booking details including addresses, weight, and contact info.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Booking confirmed with tracking number.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="404">Carrier code not found.</response>
    [HttpPost("{code}/book")]
    [ProducesResponseType(typeof(ApiResponse<BookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Book(
        [FromRoute] string code,
        [FromBody] BookShipmentRequest request,
        CancellationToken ct)
    {
        var bookingRequest = MapToBookingRequest(code, request);
        var result = await _bookingService.BookAsync(bookingRequest, ct);

        var response = new BookingResponse(result.CarrierCode, result.TrackingNumber, result.BookedAt);
        return Ok(ApiResponse<BookingResponse>.Ok(response, _correlationId.GetCorrelationId()));
    }

    /// <summary>Track a shipment by carrier code and tracking number.</summary>
    /// <remarks>
    /// Queries the carrier's tracking API for the latest shipment status and location.
    /// </remarks>
    /// <param name="code">Carrier code (e.g. "SF" or "JD").</param>
    /// <param name="trackingNo">Tracking number issued at booking.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Current tracking status.</response>
    /// <response code="404">Carrier code not found.</response>
    [HttpGet("{code}/track/{trackingNo}")]
    [ProducesResponseType(typeof(ApiResponse<CarrierTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Track(
        [FromRoute] string code,
        [FromRoute] string trackingNo,
        CancellationToken ct)
    {
        var adapter = _adapterFactory.Resolve(code);
        var tracking = await adapter.TrackAsync(trackingNo, ct);

        var response = new CarrierTrackingResponse(
            tracking.TrackingNumber,
            tracking.Status.ToString(),
            tracking.CurrentLocation,
            tracking.UpdatedAt);

        return Ok(ApiResponse<CarrierTrackingResponse>.Ok(response, _correlationId.GetCorrelationId()));
    }

    // ── Mapping helpers ──────────────────────────────────────────

    private static Domain.Carrier.Models.QuoteRequest MapToQuoteRequest(GetQuotesRequest request)
    {
        return new Domain.Carrier.Models.QuoteRequest(
            MapToAddress(request.Origin),
            MapToAddress(request.Destination),
            new Weight(request.Weight.Value, Enum.Parse<WeightUnit>(request.Weight.Unit, ignoreCase: true)),
            Enum.Parse<ServiceLevel>(request.ServiceLevel, ignoreCase: true));
    }

    private static Domain.Carrier.Models.BookingRequest MapToBookingRequest(string code, BookShipmentRequest request)
    {
        return new Domain.Carrier.Models.BookingRequest(
            code.ToUpperInvariant(),
            MapToAddress(request.Origin),
            MapToAddress(request.Destination),
            new Weight(request.Weight.Value, Enum.Parse<WeightUnit>(request.Weight.Unit, ignoreCase: true)),
            new ContactInfo(request.Sender.Name, request.Sender.Phone, request.Sender.Email),
            new ContactInfo(request.Recipient.Name, request.Recipient.Phone, request.Recipient.Email),
            Enum.Parse<ServiceLevel>(request.ServiceLevel, ignoreCase: true));
    }

    private static Address MapToAddress(AddressDto dto)
    {
        return new Address(dto.Street, dto.City, dto.Province, dto.PostalCode, dto.Country);
    }
}
