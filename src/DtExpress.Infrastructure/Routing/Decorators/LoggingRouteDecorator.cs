using System.Diagnostics;
using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Domain.Routing.Models;
using Microsoft.Extensions.Logging;

namespace DtExpress.Infrastructure.Routing.Decorators;

/// <summary>
/// Decorator Pattern: logs route calculation invocations via <see cref="ILogger"/>.
/// <para>
/// Logs before (strategy name, origin/destination) and after (elapsed time, distance, cost)
/// each route calculation. Useful for diagnostics and performance monitoring.
/// </para>
/// </summary>
public sealed class LoggingRouteDecorator : IRouteStrategy
{
    private readonly IRouteStrategy _inner;
    private readonly ILogger<LoggingRouteDecorator> _logger;

    public LoggingRouteDecorator(
        IRouteStrategy inner,
        ILogger<LoggingRouteDecorator> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public Route Calculate(RouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Route calculation started: Strategy={Strategy}, Origin=({OriginLat},{OriginLng}), Destination=({DestLat},{DestLng}), ServiceLevel={ServiceLevel}",
            Name,
            request.Origin.Latitude, request.Origin.Longitude,
            request.Destination.Latitude, request.Destination.Longitude,
            request.ServiceLevel);

        var stopwatch = Stopwatch.StartNew();

        var route = _inner.Calculate(request);

        stopwatch.Stop();

        _logger.LogInformation(
            "Route calculation completed: Strategy={Strategy}, Distance={DistanceKm:F2}km, Duration={Duration}, Cost={Cost}, Waypoints={WaypointCount}, Elapsed={ElapsedMs}ms",
            Name,
            route.DistanceKm,
            route.EstimatedDuration,
            route.EstimatedCost,
            route.WaypointNodeIds.Count,
            stopwatch.ElapsedMilliseconds);

        return route;
    }
}
