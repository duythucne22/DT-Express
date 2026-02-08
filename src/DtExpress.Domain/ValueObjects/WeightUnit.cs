namespace DtExpress.Domain.ValueObjects;

/// <summary>
/// Unit of weight measurement.
/// Supports metric (Kg, G), Chinese traditional (Jin = 0.5 kg), and imperial (Lb).
/// </summary>
public enum WeightUnit
{
    /// <summary>Kilograms — base unit.</summary>
    Kg,

    /// <summary>Grams — 1/1000 of a kilogram.</summary>
    G,

    /// <summary>斤 (Jin) — Chinese unit, equals 0.5 kg.</summary>
    Jin,

    /// <summary>Pounds — imperial unit, ~0.4536 kg.</summary>
    Lb
}
