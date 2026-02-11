using System.Net.Http.Headers;
using System.Net.Http.Json;
using DtExpress.Api.Models;
using DtExpress.Api.Models.Auth;

namespace DtExpress.Api.Tests.Fixtures;

/// <summary>
/// Helper to authenticate test users and configure HttpClient with JWT Bearer tokens.
/// </summary>
public static class TestAuthHelper
{
    // Test accounts matching DatabaseSeeder
    public static readonly TestAccount Admin = new("admin", "admin123", "Admin");
    public static readonly TestAccount Dispatcher = new("dispatcher", "passwd123", "Dispatcher");
    public static readonly TestAccount Driver = new("driver", "passwd123", "Driver");
    public static readonly TestAccount Viewer = new("viewer", "passwd123", "Viewer");

    /// <summary>Login and return the AuthResponse with tokens.</summary>
    public static async Task<AuthResponse> LoginAsync(HttpClient client, TestAccount account)
    {
        var loginRequest = new LoginRequest
        {
            Username = account.Username,
            Password = account.Password
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        return apiResponse!.Data!;
    }

    /// <summary>Login and set the Authorization header on the client. Returns the auth response.</summary>
    public static async Task<AuthResponse> AuthenticateAsync(HttpClient client, TestAccount account)
    {
        var auth = await LoginAsync(client, account);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth;
    }

    /// <summary>Set a specific token on the client.</summary>
    public static void SetToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>Clear any Authorization header from the client.</summary>
    public static void ClearToken(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
    }
}

/// <summary>Test account credentials.</summary>
public sealed record TestAccount(string Username, string Password, string Role);
