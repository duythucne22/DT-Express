namespace DtExpress.Domain.ValueObjects;

/// <summary>
/// Geographic coordinate with China-bounds awareness.
/// Uses Haversine formula for distance calculation.
/// </summary>
public sealed record GeoCoordinate
{
    /// <summary>China mainland latitude bounds.</summary>
    private const decimal ChinaMinLatitude = 3.86m;
    private const decimal ChinaMaxLatitude = 53.55m;

    /// <summary>China mainland longitude bounds.</summary>
    private const decimal ChinaMinLongitude = 73.66m;
    private const decimal ChinaMaxLongitude = 135.05m;

    /// <summary>Earth's mean radius in kilometers.</summary>
    private const double EarthRadiusKm = 6371.0;

    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }

    public GeoCoordinate(decimal latitude, decimal longitude)
    {
        if (latitude < -90m || latitude > 90m)
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90.");
        if (longitude < -180m || longitude > 180m)
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Checks whether this coordinate falls within China mainland bounds.
    /// </summary>
    public bool IsWithinChina()
        => Latitude >= ChinaMinLatitude && Latitude <= ChinaMaxLatitude
        && Longitude >= ChinaMinLongitude && Longitude <= ChinaMaxLongitude;

    /// <summary>
    /// Haversine distance to another coordinate, in kilometers.
    /// Accurate for any two points on Earth.
    /// </summary>
    public decimal DistanceToKm(GeoCoordinate other)
    {
        var lat1 = ToRadians((double)Latitude);
        var lat2 = ToRadians((double)other.Latitude);
        var dLat = ToRadians((double)(other.Latitude - Latitude));
        var dLon = ToRadians((double)(other.Longitude - Longitude));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1) * Math.Cos(lat2)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return (decimal)(EarthRadiusKm * c);
    }

    public override string ToString() => $"({Latitude:F6}, {Longitude:F6})";

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
