using DtExpress.Domain.Common;
using DtExpress.Domain.Routing.Interfaces;

namespace DtExpress.Infrastructure.Routing;

/// <summary>
/// Factory Pattern: resolves <see cref="IRouteStrategy"/> by name from a dictionary registry.
/// <para>
/// <strong>ADR-006 compliant</strong>: No switch/if-else chains. DI injects all registered
/// <see cref="IRouteStrategy"/> instances via <c>IEnumerable&lt;IRouteStrategy&gt;</c>,
/// which are indexed by <see cref="IRouteStrategy.Name"/> into a case-insensitive dictionary.
/// Adding a new strategy = new class + one DI registration line. Zero changes here.
/// </para>
/// </summary>
public sealed class RouteStrategyFactory : IRouteStrategyFactory
{
    private readonly IReadOnlyDictionary<string, IRouteStrategy> _strategies;

    /// <summary>
    /// DI injects ALL registered <see cref="IRouteStrategy"/> instances.
    /// Builds a name → strategy dictionary for O(1) lookup.
    /// </summary>
    public RouteStrategyFactory(IEnumerable<IRouteStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);

        _strategies = strategies
            .ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IRouteStrategy Create(string strategyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyName);

        // Dictionary lookup — NO switch/if-else (ADR-006)
        if (_strategies.TryGetValue(strategyName, out var strategy))
            return strategy;

        throw new StrategyNotFoundException(strategyName);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Available()
        => _strategies.Keys.ToList().AsReadOnly();
}
