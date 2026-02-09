using DtExpress.Domain.Routing.Models;

namespace DtExpress.Domain.Routing.Interfaces;

/// <summary>
/// Factory Pattern: creates strategies by name from a registry.
/// No switch/if-else — uses dictionary populated by DI.
/// </summary>
public interface IRouteStrategyFactory
{
    /// <summary>Create (resolve) a strategy by its registered name.</summary>
    IRouteStrategy Create(string strategyName);

    /// <summary>List all available strategy names for comparison/discovery.</summary>
    IReadOnlyList<string> Available();
}
