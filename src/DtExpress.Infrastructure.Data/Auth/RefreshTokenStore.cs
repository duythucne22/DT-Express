using System.Collections.Concurrent;

namespace DtExpress.Infrastructure.Data.Auth;

/// <summary>
/// In-memory refresh token store. Maps refresh tokens to user IDs with expiration.
/// Tokens are lost on application restart (users must re-login).
/// <para>
/// Future enhancement: persist to a <c>refresh_tokens</c> database table.
/// </para>
/// </summary>
public sealed class RefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshTokenEntry> _tokens = new();

    /// <summary>Store a refresh token for a user with expiration.</summary>
    public void Store(string refreshToken, Guid userId, DateTimeOffset expiresAt)
    {
        // Remove any existing tokens for this user (one active refresh token per user)
        foreach (var kvp in _tokens)
        {
            if (kvp.Value.UserId == userId)
                _tokens.TryRemove(kvp.Key, out _);
        }

        _tokens[refreshToken] = new RefreshTokenEntry(userId, expiresAt);
    }

    /// <summary>Validate and consume a refresh token. Returns the user ID if valid.</summary>
    public Guid? Validate(string refreshToken)
    {
        if (!_tokens.TryRemove(refreshToken, out var entry))
            return null;

        if (entry.ExpiresAt < DateTimeOffset.UtcNow)
            return null; // Expired

        return entry.UserId;
    }

    /// <summary>Revoke all refresh tokens for a user (e.g. on password change).</summary>
    public void RevokeAll(Guid userId)
    {
        foreach (var kvp in _tokens)
        {
            if (kvp.Value.UserId == userId)
                _tokens.TryRemove(kvp.Key, out _);
        }
    }

    private sealed record RefreshTokenEntry(Guid UserId, DateTimeOffset ExpiresAt);
}
