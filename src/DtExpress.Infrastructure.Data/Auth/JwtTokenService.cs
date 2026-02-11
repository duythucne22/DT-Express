using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DtExpress.Application.Auth.Models;
using DtExpress.Infrastructure.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace DtExpress.Infrastructure.Data.Auth;

/// <summary>
/// Generates JWT access tokens and opaque refresh tokens.
/// Access tokens contain claims: sub, unique_name, email, role, display_name.
/// Refresh tokens are cryptographically random Base64 strings.
/// </summary>
public sealed class JwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(JwtSettings settings)
    {
        _settings = settings;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    /// <summary>Generate a JWT access token for the given user.</summary>
    public (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(UserEntity user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("display_name", user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <summary>Generate a cryptographically random refresh token.</summary>
    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
