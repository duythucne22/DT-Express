using DtExpress.Application.Auth.Models;
using DtExpress.Application.Auth.Services;
using DtExpress.Domain.Common;
using DtExpress.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DtExpress.Infrastructure.Data.Auth;

/// <summary>
/// Authentication service: login, register, refresh token.
/// Uses BCrypt for password hashing, JWT for token generation.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwtService;
    private readonly RefreshTokenStore _refreshStore;
    private readonly JwtSettings _settings;

    /// <summary>Valid TMS roles matching the database CHECK constraint.</summary>
    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "Dispatcher", "Driver", "Viewer"
    };

    public AuthService(
        AppDbContext db,
        JwtTokenService jwtService,
        RefreshTokenStore refreshStore,
        JwtSettings settings)
    {
        _db = db;
        _jwtService = jwtService;
        _refreshStore = refreshStore;
        _settings = settings;
    }

    /// <inheritdoc />
    public async Task<AuthResult> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user is null)
            throw new DomainException("AUTH_FAILED", "Invalid username or password.");

        if (!user.IsActive)
            throw new DomainException("ACCOUNT_DISABLED", "This account has been deactivated.");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new DomainException("AUTH_FAILED", "Invalid username or password.");

        return GenerateTokens(user);
    }

    /// <inheritdoc />
    public async Task<AuthResult> RegisterAsync(
        string username,
        string email,
        string password,
        string displayName,
        string role,
        CancellationToken ct = default)
    {
        // Normalize role to PascalCase to match CHECK constraint
        var normalizedRole = ValidRoles.FirstOrDefault(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
        if (normalizedRole is null)
            throw new DomainException("INVALID_ROLE", $"Invalid role '{role}'. Valid roles: {string.Join(", ", ValidRoles)}.");

        // Check uniqueness
        var existingUsername = await _db.Users
            .AnyAsync(u => u.Username == username, ct);
        if (existingUsername)
            throw new DomainException("USERNAME_TAKEN", $"Username '{username}' is already taken.");

        var existingEmail = await _db.Users
            .AnyAsync(u => u.Email == email, ct);
        if (existingEmail)
            throw new DomainException("EMAIL_TAKEN", $"Email '{email}' is already in use.");

        // Create user with BCrypt hash (work factor 12)
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            DisplayName = displayName,
            Role = normalizedRole,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return GenerateTokens(user);
    }

    /// <inheritdoc />
    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var userId = _refreshStore.Validate(refreshToken);
        if (userId is null)
            throw new DomainException("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired.");

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, ct);

        if (user is null || !user.IsActive)
            throw new DomainException("AUTH_FAILED", "User not found or account deactivated.");

        return GenerateTokens(user);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private AuthResult GenerateTokens(UserEntity user)
    {
        var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenExpirationDays);
        _refreshStore.Store(refreshToken, user.Id, refreshExpiresAt);

        return new AuthResult(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: expiresAt,
            UserId: user.Id.ToString(),
            Username: user.Username,
            DisplayName: user.DisplayName,
            Role: user.Role);
    }
}
