using System.Security.Claims;
using DtExpress.Application.Auth.Services;

namespace DtExpress.Api.Auth;

/// <summary>
/// Reads current user identity from HttpContext.User (JWT claims).
/// Registered as Scoped — one per HTTP request.
/// Returns null values for anonymous requests.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public string? UserName => User?.FindFirstValue(ClaimTypes.Name)
                               ?? User?.FindFirstValue("unique_name");

    /// <inheritdoc />
    public string? DisplayName => User?.FindFirstValue("display_name");

    /// <inheritdoc />
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
}
