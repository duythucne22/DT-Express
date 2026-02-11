using System.Net;
using System.Net.Http.Json;
using DtExpress.Api.Models;
using DtExpress.Api.Models.Auth;
using DtExpress.Api.Tests.Fixtures;

namespace DtExpress.Api.Tests;

/// <summary>
/// JWT Authentication test suite:
/// - Successful login for each role (admin, dispatcher, driver, viewer)
/// - Token refresh mechanism
/// - Invalid credentials rejection
/// - Expired/malformed token handling
/// - Role-based access control (RBAC)
/// </summary>
public sealed class AuthenticationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthenticationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ═════════════════════════════════════════════════════════════
    //  1. Successful login for each role
    // ═════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("admin", "admin123", "Admin")]
    [InlineData("dispatcher", "passwd123", "Dispatcher")]
    [InlineData("driver", "passwd123", "Driver")]
    [InlineData("viewer", "passwd123", "Viewer")]
    public async Task Login_WithValidCredentials_ReturnsTokens(string username, string password, string expectedRole)
    {
        // Arrange
        var request = new LoginRequest { Username = username, Password = password };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.False(string.IsNullOrEmpty(apiResponse.Data.AccessToken));
        Assert.False(string.IsNullOrEmpty(apiResponse.Data.RefreshToken));
        Assert.Equal(username, apiResponse.Data.Username);
        Assert.Equal(expectedRole, apiResponse.Data.Role);
        Assert.True(apiResponse.Data.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task Login_ReturnsValidUserId()
    {
        // Arrange & Act
        var auth = await TestAuthHelper.LoginAsync(_client, TestAuthHelper.Admin);

        // Assert
        Assert.True(Guid.TryParse(auth.UserId, out _), "UserId should be a valid GUID");
    }

    // ═════════════════════════════════════════════════════════════
    //  2. Token refresh mechanism
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokenPair()
    {
        // Arrange — login first to get a refresh token
        var auth = await TestAuthHelper.LoginAsync(_client, TestAuthHelper.Admin);

        // Act — use the refresh token to get a new pair
        var refreshRequest = new RefreshTokenRequest { RefreshToken = auth.RefreshToken };
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.False(string.IsNullOrEmpty(apiResponse.Data.AccessToken));
        Assert.False(string.IsNullOrEmpty(apiResponse.Data.RefreshToken));
        // New tokens should be different from old ones
        Assert.NotEqual(auth.RefreshToken, apiResponse.Data.RefreshToken);
    }

    [Fact]
    public async Task Refresh_TokenIsSingleUse()
    {
        // Arrange — login and get a refresh token
        var auth = await TestAuthHelper.LoginAsync(_client, TestAuthHelper.Admin);
        var refreshRequest = new RefreshTokenRequest { RefreshToken = auth.RefreshToken };

        // Act — first refresh succeeds
        var firstResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act — second refresh with same token should fail (single-use)
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert — DomainException mapped to 400 by GlobalExceptionFilter
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var apiResponse = await secondResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.NotNull(apiResponse.Error);
        Assert.Equal("INVALID_REFRESH_TOKEN", apiResponse.Error.Code);
    }

    // ═════════════════════════════════════════════════════════════
    //  3. Invalid credentials rejection
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_WithWrongPassword_Returns400()
    {
        // Arrange
        var request = new LoginRequest { Username = "admin", Password = "wrongpassword" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert — DomainException("AUTH_FAILED") → 400
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("AUTH_FAILED", apiResponse.Error!.Code);
    }

    [Fact]
    public async Task Login_WithNonexistentUser_Returns400()
    {
        // Arrange
        var request = new LoginRequest { Username = "nobody", Password = "password" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Equal("AUTH_FAILED", apiResponse!.Error!.Code);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns400()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest { RefreshToken = "totally-invalid-refresh-token" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Equal("INVALID_REFRESH_TOKEN", apiResponse!.Error!.Code);
    }

    // ═════════════════════════════════════════════════════════════
    //  4. Expired/malformed token handling
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProtectedEndpoint_WithMalformedToken_Returns401()
    {
        // Arrange
        TestAuthHelper.SetToken(_client, "this-is-not-a-jwt-token");

        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Cleanup
        TestAuthHelper.ClearToken(_client);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        // Arrange
        TestAuthHelper.ClearToken(_client);

        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════
    //  5. Registration
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task Register_WithValidData_ReturnsTokens()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = $"newuser_{Guid.NewGuid():N}",
            Email = $"new_{Guid.NewGuid():N}@test.com",
            Password = "NewPass123!",
            DisplayName = "Test User",
            Role = "Viewer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.True(apiResponse!.Success);
        Assert.Equal(request.Username, apiResponse.Data!.Username);
        Assert.Equal("Viewer", apiResponse.Data.Role);
        Assert.False(string.IsNullOrEmpty(apiResponse.Data.AccessToken));
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_Returns400()
    {
        // Arrange — "admin" already exists from seed
        var request = new RegisterRequest
        {
            Username = "admin",
            Email = "unique@test.com",
            Password = "Pass123!",
            DisplayName = "Dup",
            Role = "Viewer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Equal("USERNAME_TAKEN", apiResponse!.Error!.Code);
    }

    [Fact]
    public async Task Register_WithInvalidRole_Returns400()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = $"badrole_{Guid.NewGuid():N}",
            Email = $"badrole_{Guid.NewGuid():N}@test.com",
            Password = "Pass123!",
            DisplayName = "Bad Role",
            Role = "SuperAdmin"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Equal("INVALID_ROLE", apiResponse!.Error!.Code);
    }

    // ═════════════════════════════════════════════════════════════
    //  6. Correlation ID flows through auth endpoints
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_ReturnsCorrelationId()
    {
        // Arrange
        var request = new LoginRequest { Username = "admin", Password = "admin123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.False(string.IsNullOrEmpty(apiResponse!.CorrelationId));

        // Also check header
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task Login_WithCustomCorrelationId_EchoesBack()
    {
        // Arrange
        var correlationId = $"test-{Guid.NewGuid()}";
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        request.Headers.Add("X-Correlation-ID", correlationId);
        request.Content = JsonContent.Create(new LoginRequest { Username = "admin", Password = "admin123" });

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").First());

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.Equal(correlationId, apiResponse!.CorrelationId);
    }
}
