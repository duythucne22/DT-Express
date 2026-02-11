using DtExpress.Application.Auth.Models;

namespace DtExpress.Application.Auth.Services;

/// <summary>
/// Authentication service: login, register, refresh token.
/// Implementation lives in Infrastructure.Data (needs DB access + BCrypt).
/// </summary>
public interface IAuthService
{
    /// <summary>Authenticate with username and password. Returns JWT tokens.</summary>
    Task<AuthResult> LoginAsync(string username, string password, CancellationToken ct = default);

    /// <summary>Register a new user. Returns JWT tokens for immediate login.</summary>
    Task<AuthResult> RegisterAsync(
        string username,
        string email,
        string password,
        string displayName,
        string role,
        CancellationToken ct = default);

    /// <summary>Exchange a valid refresh token for a new access token pair.</summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}
