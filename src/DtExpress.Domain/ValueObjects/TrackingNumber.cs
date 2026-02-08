using System.Text.RegularExpressions;

namespace DtExpress.Domain.ValueObjects;

/// <summary>
/// Tracking number value wrapper with Chinese carrier format validation.
/// SF Express: exactly 12 digits. JD Logistics: 10-15 alphanumeric characters.
/// Generic: minimum 6 characters.
/// </summary>
public sealed record TrackingNumber
{
    /// <summary>SF Express: exactly 12 digits.</summary>
    private static readonly Regex SfPattern = new(@"^\d{12}$", RegexOptions.Compiled);

    /// <summary>JD Logistics: 10-15 alphanumeric characters.</summary>
    private static readonly Regex JdPattern = new(@"^[A-Za-z0-9]{10,15}$", RegexOptions.Compiled);

    public string Value { get; init; }

    public TrackingNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Tracking number is required.", nameof(value));
        if (value.Length < 6)
            throw new ArgumentException("Tracking number must be at least 6 characters.", nameof(value));

        Value = value;
    }

    /// <summary>Create and validate as SF Express tracking number (12 digits).</summary>
    public static TrackingNumber ForSF(string value)
    {
        if (!SfPattern.IsMatch(value))
            throw new ArgumentException($"SF Express tracking number must be exactly 12 digits. Got: '{value}'.", nameof(value));
        return new TrackingNumber(value);
    }

    /// <summary>Create and validate as JD Logistics tracking number (10-15 alphanumeric).</summary>
    public static TrackingNumber ForJD(string value)
    {
        if (!JdPattern.IsMatch(value))
            throw new ArgumentException($"JD tracking number must be 10-15 alphanumeric characters. Got: '{value}'.", nameof(value));
        return new TrackingNumber(value);
    }

    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for convenience.</summary>
    public static implicit operator string(TrackingNumber tn) => tn.Value;
}
