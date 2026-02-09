using System.Diagnostics;
using DtExpress.Domain.Carrier.Interfaces;
using DtExpress.Domain.Carrier.Models;
using Microsoft.Extensions.Logging;

namespace DtExpress.Infrastructure.Carrier.Decorators;

/// <summary>
/// Decorator Pattern: logs all carrier adapter operations with timing.
/// Wraps any <see cref="ICarrierAdapter"/> with structured ILogger output.
/// <para>
/// Logs: carrier code, operation type, request details, response summary, elapsed time.
/// </para>
/// </summary>
public sealed class LoggingCarrierDecorator : ICarrierAdapter
{
    private readonly ICarrierAdapter _inner;
    private readonly ILogger<LoggingCarrierDecorator> _logger;

    public LoggingCarrierDecorator(ICarrierAdapter inner, ILogger<LoggingCarrierDecorator> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string CarrierCode => _inner.CarrierCode;

    /// <inheritdoc />
    public async Task<CarrierQuote> GetQuoteAsync(QuoteRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Carrier:{CarrierCode}] GetQuote — {Origin} → {Destination}, {Weight}kg, {ServiceLevel}",
            _inner.CarrierCode,
            request.Origin.City,
            request.Destination.City,
            request.Weight.ToKilograms(),
            request.ServiceLevel);

        var sw = Stopwatch.StartNew();
        var quote = await _inner.GetQuoteAsync(request, ct);
        sw.Stop();

        _logger.LogInformation(
            "[Carrier:{CarrierCode}] Quote result — {Price}, {Days} days, elapsed {ElapsedMs}ms",
            _inner.CarrierCode,
            quote.Price,
            quote.EstimatedDays,
            sw.ElapsedMilliseconds);

        return quote;
    }

    /// <inheritdoc />
    public async Task<BookingResult> BookAsync(BookingRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Carrier:{CarrierCode}] Book — {Origin} → {Destination}, {Weight}kg, sender={Sender}",
            _inner.CarrierCode,
            request.Origin.City,
            request.Destination.City,
            request.Weight.ToKilograms(),
            request.Sender.Name);

        var sw = Stopwatch.StartNew();
        var result = await _inner.BookAsync(request, ct);
        sw.Stop();

        _logger.LogInformation(
            "[Carrier:{CarrierCode}] Booked — tracking={TrackingNumber}, elapsed {ElapsedMs}ms",
            _inner.CarrierCode,
            result.TrackingNumber,
            sw.ElapsedMilliseconds);

        return result;
    }

    /// <inheritdoc />
    public async Task<CarrierTrackingInfo> TrackAsync(string trackingNumber, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Carrier:{CarrierCode}] Track — trackingNumber={TrackingNumber}",
            _inner.CarrierCode,
            trackingNumber);

        var sw = Stopwatch.StartNew();
        var info = await _inner.TrackAsync(trackingNumber, ct);
        sw.Stop();

        _logger.LogInformation(
            "[Carrier:{CarrierCode}] Tracking result — status={Status}, location={Location}, elapsed {ElapsedMs}ms",
            _inner.CarrierCode,
            info.Status,
            info.CurrentLocation,
            sw.ElapsedMilliseconds);

        return info;
    }
}
