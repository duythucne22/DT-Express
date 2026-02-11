using DtExpress.Api.Models;
using DtExpress.Api.Models.Auth;
using DtExpress.Application.Auth.Models;
using DtExpress.Application.Auth.Services;
using DtExpress.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DtExpress.Api.Controllers;

/// <summary>
/// Authentication endpoints: login, register, refresh token.
/// All endpoints are anonymous (no JWT required).
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Tags("Authentication")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICorrelationIdProvider _correlationId;

    public AuthController(IAuthService authService, ICorrelationIdProvider correlationId)
    {
        _authService = authService;
        _correlationId = correlationId;
    }

    /// <summary>Login with username and password.</summary>
    /// <remarks>
    /// Returns a JWT access token (15 min) and a refresh token (7 days).
    /// Include the access token in subsequent requests: <c>Authorization: Bearer {token}</c>.
    ///
    /// **Test accounts** (seeded):
    /// | Username | Password | Role |
    /// |---|---|---|
    /// | admin | admin123 | Admin |
    /// | dispatcher | passwd123 | Dispatcher |
    /// | driver | passwd123 | Driver |
    /// | viewer | passwd123 | Viewer |
    /// </remarks>
    /// <response code="200">Authentication successful with tokens.</response>
    /// <response code="401">Invalid credentials or account disabled.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request.Username, request.Password, ct);
        return Ok(ApiResponse<AuthResponse>.Ok(MapToResponse(result), _correlationId.GetCorrelationId()));
    }

    /// <summary>Register a new user.</summary>
    /// <remarks>
    /// Creates a new user account and returns JWT tokens for immediate use.
    /// Valid roles: **Admin**, **Dispatcher**, **Driver**, **Viewer**.
    ///
    /// Passwords are hashed with BCrypt (work factor 12).
    /// </remarks>
    /// <response code="201">User created with tokens.</response>
    /// <response code="400">Validation error (duplicate username/email, invalid role).</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(
            request.Username,
            request.Email,
            request.Password,
            request.DisplayName,
            request.Role,
            ct);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<AuthResponse>.Ok(MapToResponse(result), _correlationId.GetCorrelationId()));
    }

    /// <summary>Refresh an expired access token.</summary>
    /// <remarks>
    /// Exchange a valid refresh token for a new access token + refresh token pair.
    /// The old refresh token is consumed (single-use) to prevent token replay.
    /// </remarks>
    /// <response code="200">New token pair.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ct);
        return Ok(ApiResponse<AuthResponse>.Ok(MapToResponse(result), _correlationId.GetCorrelationId()));
    }

    // ── Mapping ─────────────────────────────────────────────────

    private static AuthResponse MapToResponse(AuthResult result) => new()
    {
        AccessToken = result.AccessToken,
        RefreshToken = result.RefreshToken,
        ExpiresAt = result.ExpiresAt,
        UserId = result.UserId,
        Username = result.Username,
        DisplayName = result.DisplayName,
        Role = result.Role,
    };
}
