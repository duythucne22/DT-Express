namespace DtExpress.Application.Auth.Services;

/// <summary>
/// Provides the current authenticated user context.
/// Implemented in the Api layer (reads HttpContext.User claims).
/// Used by repositories and audit system to stamp user identity.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>User ID from JWT "sub" claim. Null if anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>Username from JWT "unique_name" claim.</summary>
    string? UserName { get; }

    /// <summary>Display name from JWT "display_name" claim.</summary>
    string? DisplayName { get; }

    /// <summary>Role from JWT "role" claim (Admin, Dispatcher, Driver, Viewer).</summary>
    string? Role { get; }

    /// <summary>True if the request has a valid JWT token.</summary>
    bool IsAuthenticated { get; }
}
