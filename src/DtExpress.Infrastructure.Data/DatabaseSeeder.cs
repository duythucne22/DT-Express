using DtExpress.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DtExpress.Infrastructure.Data;

/// <summary>
/// Development data seeder: ensures test accounts exist with proper BCrypt hashes.
/// Called on startup in Development mode only.
/// <para>
/// This replaces manual BCrypt hash generation in seed-data.sql.
/// Test accounts are upserted (created if missing, left alone if present).
/// </para>
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seed test users with proper BCrypt password hashes.
    /// Only creates users that don't already exist (safe to call multiple times).
    /// </summary>
    public static async Task SeedTestUsersAsync(AppDbContext db)
    {
        var testUsers = new[]
        {
            new UserSeed("a0000000-0000-0000-0000-000000000001", "admin",      "admin@dtexpress.com",      "admin123",    "系统管理员", "Admin"),
            new UserSeed("a0000000-0000-0000-0000-000000000002", "dispatcher",  "dispatcher@dtexpress.com", "passwd123", "调度员小李", "Dispatcher"),
            new UserSeed("a0000000-0000-0000-0000-000000000003", "driver",      "driver@dtexpress.com",     "passwd123",   "司机王师傅", "Driver"),
            new UserSeed("a0000000-0000-0000-0000-000000000004", "viewer",      "viewer@dtexpress.com",     "passwd123",   "客服张小姐", "Viewer"),
        };

        foreach (var seed in testUsers)
        {
            var exists = await db.Users.AnyAsync(u => u.Username == seed.Username);
            if (exists) continue;

            db.Users.Add(new UserEntity
            {
                Id = Guid.Parse(seed.Id),
                Username = seed.Username,
                Email = seed.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(seed.Password, workFactor: 12),
                DisplayName = seed.DisplayName,
                Role = seed.Role,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }

        await db.SaveChangesAsync();
    }

    private sealed record UserSeed(string Id, string Username, string Email, string Password, string DisplayName, string Role);
}
