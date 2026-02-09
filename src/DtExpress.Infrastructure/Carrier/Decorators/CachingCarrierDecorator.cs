using System.Collections.Concurrent;
using DtExpress.Domain.Carrier.Interfaces;
using DtExpress.Domain.Carrier.Models;

namespace DtExpress.Infrastructure.Carrier.Decorators;

/// <summary>
/// Decorator Pattern: caches carrier quotes by request hash.
/// Wraps any <see cref="ICarrierAdapter"/> to avoid redundant quote API calls.
/// <para>
/// Cache key: deterministic hash of (CarrierCode, Origin.City, Dest.City, Weight, ServiceLevel).
/// Only <see cref="GetQuoteAsync"/> is cached — booking and tracking are always live.
/// </para>
/// </summary>
public sealed class CachingCarrierDecorator : ICarrierAdapter
{
    private readonly ICarrierAdapter _inner;
    private readonly ConcurrentDictionary<string, CarrierQuote> _quoteCache = new();

    public CachingCarrierDecorator(ICarrierAdapter inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public string CarrierCode => _inner.CarrierCode;

    /// <inheritdoc />
    /// <remarks>Cached: returns stored quote if request hash matches a previous call.</remarks>
    public async Task<CarrierQuote> GetQuoteAsync(QuoteRequest request, CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(request);

        if (_quoteCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var quote = await _inner.GetQuoteAsync(request, ct);
        _quoteCache.TryAdd(cacheKey, quote);
        return quote;
    }

    /// <inheritdoc />
    /// <remarks>Not cached: bookings must always go through to the carrier.</remarks>
    public Task<BookingResult> BookAsync(BookingRequest request, CancellationToken ct = default)
        => _inner.BookAsync(request, ct);

    /// <inheritdoc />
    /// <remarks>Not cached: tracking status changes over time.</remarks>
    public Task<CarrierTrackingInfo> TrackAsync(string trackingNumber, CancellationToken ct = default)
        => _inner.TrackAsync(trackingNumber, ct);

    /// <summary>
    /// Build a deterministic cache key from the quote request properties.
    /// </summary>
    private string BuildCacheKey(QuoteRequest request)
        => $"{_inner.CarrierCode}|{request.Origin.City}|{request.Destination.City}|{request.Weight.ToKilograms():F2}|{request.ServiceLevel}";
}
