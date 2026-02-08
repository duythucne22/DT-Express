namespace DtExpress.Domain.ValueObjects;

/// <summary>
/// Package dimensions in centimeters. Computes volumetric weight
/// using Chinese logistics factors: 5000 (air), 6000 (ground).
/// </summary>
public sealed record Dimension
{
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }

    public Dimension(decimal lengthCm, decimal widthCm, decimal heightCm)
    {
        if (lengthCm <= 0)
            throw new ArgumentException("Length must be positive.", nameof(lengthCm));
        if (widthCm <= 0)
            throw new ArgumentException("Width must be positive.", nameof(widthCm));
        if (heightCm <= 0)
            throw new ArgumentException("Height must be positive.", nameof(heightCm));

        LengthCm = lengthCm;
        WidthCm = widthCm;
        HeightCm = heightCm;
    }

    /// <summary>Volume in cubic centimeters.</summary>
    public decimal VolumeCm3 => LengthCm * WidthCm * HeightCm;

    /// <summary>
    /// Volumetric weight using the given divisor factor.
    /// Chinese standard: 5000 for air freight, 6000 for ground freight.
    /// Formula: Length × Width × Height / factor.
    /// </summary>
    public Weight VolumetricWeight(int factor = 5000)
    {
        if (factor <= 0)
            throw new ArgumentException("Factor must be positive.", nameof(factor));
        return Weight.Kilograms(VolumeCm3 / factor);
    }

    /// <summary>Volumetric weight for air freight (factor = 5000).</summary>
    public Weight VolumetricWeightAir() => VolumetricWeight(5000);

    /// <summary>Volumetric weight for ground freight (factor = 6000).</summary>
    public Weight VolumetricWeightGround() => VolumetricWeight(6000);

    /// <summary>Check if this dimension fits within a container.</summary>
    public bool FitsWithin(Dimension container)
        => LengthCm <= container.LengthCm
        && WidthCm <= container.WidthCm
        && HeightCm <= container.HeightCm;

    public override string ToString() => $"{LengthCm}×{WidthCm}×{HeightCm} cm";
}
