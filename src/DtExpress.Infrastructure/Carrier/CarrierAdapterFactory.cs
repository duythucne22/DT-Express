using DtExpress.Domain.Carrier.Interfaces;
using DtExpress.Domain.Common;

namespace DtExpress.Infrastructure.Carrier;

/// <summary>
/// Factory Pattern: registry-based carrier adapter resolution by carrier code.
/// <para>
/// Built from <see cref="IEnumerable{ICarrierAdapter}"/> injected by DI —
/// no switch/if-else branching (ADR-006). Dictionary lookup with
/// <see cref="StringComparer.OrdinalIgnoreCase"/> for case-insensitive resolution.
/// </para>
/// <para>
/// Adding a new carrier requires only:
/// 1. A new <see cref="ICarrierAdapter"/> implementation
/// 2. One DI registration line → factory auto-discovers it
/// </para>
/// </summary>
public sealed class CarrierAdapterFactory : ICarrierAdapterFactory
{
    private readonly IReadOnlyDictionary<string, ICarrierAdapter> _adapters;

    /// <summary>
    /// Build the adapter registry from all DI-registered <see cref="ICarrierAdapter"/> instances.
    /// </summary>
    public CarrierAdapterFactory(IEnumerable<ICarrierAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);

        _adapters = adapters.ToDictionary(
            a => a.CarrierCode,
            a => a,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dictionary lookup — O(1). Throws <see cref="CarrierNotFoundException"/> if code not registered.
    /// </remarks>
    public ICarrierAdapter Resolve(string carrierCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carrierCode);

        if (_adapters.TryGetValue(carrierCode, out var adapter))
        {
            return adapter;
        }

        throw new CarrierNotFoundException(carrierCode);
    }

    /// <inheritdoc />
    public IReadOnlyList<ICarrierAdapter> GetAll()
        => _adapters.Values.ToList().AsReadOnly();
}
