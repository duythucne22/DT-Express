namespace DtExpress.Infrastructure.Data.Entities;

/// <summary>
/// EF entity mapping to the <c>users</c> table.
/// Persistence-only — not a domain model.
/// </summary>
public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Role { get; set; } = null!; // CHECK: Admin, Dispatcher, Driver, Viewer
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation: orders created by this user
    public ICollection<OrderEntity> Orders { get; set; } = new List<OrderEntity>();
    // Navigation: audit logs by this user
    public ICollection<AuditLogEntity> AuditLogs { get; set; } = new List<AuditLogEntity>();
}
