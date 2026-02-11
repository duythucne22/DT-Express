namespace DtExpress.Application.Auth.Models;

/// <summary>
/// Result of a successful login or registration.
/// Contains both access token (short-lived) and refresh token (long-lived).
/// </summary>
public sealed record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string UserId,
    string Username,
    string DisplayName,
    string Role);
