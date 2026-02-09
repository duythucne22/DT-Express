namespace DtExpress.Domain.Carrier.Interfaces;

/// <summary>
/// Factory Pattern: registry-based adapter resolution by carrier code.
/// No switch/if-else — dictionary built from DI IEnumerable injection.
/// </summary>
public interface ICarrierAdapterFactory
{
    /// <summary>Resolve adapter by carrier code (case-insensitive). Throws if not found.</summary>
    ICarrierAdapter Resolve(string carrierCode);

    /// <summary>Get all registered carrier adapters.</summary>
    IReadOnlyList<ICarrierAdapter> GetAll();
}
