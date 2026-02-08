namespace DtExpress.Domain.ValueObjects;

/// <summary>
/// Package weight with unit conversion.
/// Supports Kg, G, Jin (斤 = 0.5 kg), and Lb.
/// </summary>
public sealed record Weight
{
    public decimal Value { get; init; }
    public WeightUnit Unit { get; init; }

    public Weight(decimal value, WeightUnit unit)
    {
        if (value <= 0)
            throw new ArgumentException("Weight must be positive.", nameof(value));

        Value = value;
        Unit = unit;
    }

    /// <summary>Create weight in kilograms.</summary>
    public static Weight Kilograms(decimal kg) => new(kg, WeightUnit.Kg);

    /// <summary>Create weight in grams.</summary>
    public static Weight Grams(decimal g) => new(g, WeightUnit.G);

    /// <summary>Create weight in 斤 (Jin).</summary>
    public static Weight Jin(decimal jin) => new(jin, WeightUnit.Jin);

    /// <summary>
    /// Convert to kilograms regardless of source unit.
    /// Jin (斤) = 0.5 kg (Chinese traditional unit).
    /// </summary>
    public decimal ToKilograms() => Unit switch
    {
        WeightUnit.Kg  => Value,
        WeightUnit.G   => Value / 1000m,
        WeightUnit.Jin => Value * 0.5m,
        WeightUnit.Lb  => Value * 0.453592m,
        _ => throw new InvalidOperationException($"Unknown weight unit: {Unit}")
    };

    /// <summary>Add two weights (converts to Kg).</summary>
    public Weight Add(Weight other)
        => Kilograms(ToKilograms() + other.ToKilograms());

    public override string ToString() => $"{Value} {Unit}";
}
