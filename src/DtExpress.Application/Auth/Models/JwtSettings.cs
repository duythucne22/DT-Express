namespace DtExpress.Application.Auth.Models;

/// <summary>
/// JWT configuration settings. Bound from appsettings.json "Jwt" section.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 secret key (min 32 characters for 256-bit security).</summary>
    public string Secret { get; set; } = null!;

    /// <summary>Token issuer (iss claim).</summary>
    public string Issuer { get; set; } = null!;

    /// <summary>Token audience (aud claim).</summary>
    public string Audience { get; set; } = null!;

    /// <summary>Access token lifetime in minutes (default: 15).</summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>Refresh token lifetime in days (default: 7).</summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
