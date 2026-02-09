using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Domain.Routing.Models;

namespace DtExpress.Infrastructure.Routing.Decorators;

/// <summary>
/// Decorator Pattern: validates <see cref="RouteRequest"/> before delegating to the inner strategy.
/// <para>
/// Checks for null request, valid origin/destination coordinates, and positive weight.
/// If any validation fails, throws <see cref="ArgumentException"/> before the inner
/// strategy is invoked — protecting against invalid data reaching the pathfinder.
/// </para>
/// </summary>
public sealed class ValidationRouteDecorator : IRouteStrategy
{
    private readonly IRouteStrategy _inner;

    public ValidationRouteDecorator(IRouteStrategy inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public Route Calculate(RouteRequest request)
    {
        Validate(request);

        return _inner.Calculate(request);
    }

    /// <summary>
    /// Validate the route request. Value objects enforce their own invariants on construction,
    /// but this decorator guards against null composition and logical issues.
    /// </summary>
    private static void Validate(RouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Origin, "request.Origin");
        ArgumentNullException.ThrowIfNull(request.Destination, "request.Destination");
        ArgumentNullException.ThrowIfNull(request.PackageWeight, "request.PackageWeight");

        // Ensure origin and destination are not the same point
        if (request.Origin == request.Destination)
            throw new ArgumentException(
                "Origin and Destination must be different coordinates.",
                nameof(request));

        // Weight must be a positive value (already enforced by Weight constructor, but double-check)
        if (request.PackageWeight.Value <= 0)
            throw new ArgumentException(
                "Package weight must be positive.",
                nameof(request));
    }
}
