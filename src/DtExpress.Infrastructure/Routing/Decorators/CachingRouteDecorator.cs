using System.Collections.Concurrent;
using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Domain.Routing.Models;

namespace DtExpress.Infrastructure.Routing.Decorators;

/// <summary>
/// Decorator Pattern: caches route calculation results in a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// <para>
/// Wraps any <see cref="IRouteStrategy"/> implementation. If the same <see cref="RouteRequest"/>
/// is seen again, the cached <see cref="Route"/> is returned without recomputation.
/// Cache key is derived from request properties for deterministic lookup.
/// </para>
/// </summary>
public sealed class CachingRouteDecorator : IRouteStrategy
{
    private readonly IRouteStrategy _inner;
    private readonly ConcurrentDictionary<string, Route> _cache;

    public CachingRouteDecorator(
        IRouteStrategy inner,
        ConcurrentDictionary<string, Route> cache)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public Route Calculate(RouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cacheKey = BuildCacheKey(request);

        return _cache.GetOrAdd(cacheKey, _ => _inner.Calculate(request));
    }

    /// <summary>
    /// Build a deterministic cache key from the request properties.
    /// Format: "Name|OriginLat,OriginLng|DestLat,DestLng|Weight|ServiceLevel"
    /// </summary>
    private string BuildCacheKey(RouteRequest request)
        => string.Join('|',
            Name,
            $"{request.Origin.Latitude},{request.Origin.Longitude}",
            $"{request.Destination.Latitude},{request.Destination.Longitude}",
            $"{request.PackageWeight.Value}{request.PackageWeight.Unit}",
            request.ServiceLevel);
}
