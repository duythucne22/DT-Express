namespace DtExpress.Api.Models.Auth;

/// <summary>Login request DTO.</summary>
public sealed class LoginRequest
{
    /// <summary>Username (unique identifier).</summary>
    public string Username { get; set; } = null!;

    /// <summary>Plain-text password.</summary>
    public string Password { get; set; } = null!;
}

/// <summary>Registration request DTO.</summary>
public sealed class RegisterRequest
{
    /// <summary>Unique username (alphanumeric).</summary>
    public string Username { get; set; } = null!;

    /// <summary>Unique email address.</summary>
    public string Email { get; set; } = null!;

    /// <summary>Plain-text password (min 6 characters recommended).</summary>
    public string Password { get; set; } = null!;

    /// <summary>Human-readable display name (shown in audit trail).</summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>Role: Admin, Dispatcher, Driver, Viewer.</summary>
    public string Role { get; set; } = null!;
}

/// <summary>Refresh token request DTO.</summary>
public sealed class RefreshTokenRequest
{
    /// <summary>The refresh token received from login/register.</summary>
    public string RefreshToken { get; set; } = null!;
}

/// <summary>Authentication response DTO (returned by login, register, refresh).</summary>
public sealed class AuthResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public string UserId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Role { get; set; } = null!;
}
