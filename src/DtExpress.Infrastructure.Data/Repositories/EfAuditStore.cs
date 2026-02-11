using System.Text.Json;
using DtExpress.Application.Auth.Services;
using DtExpress.Domain.Audit.Enums;
using DtExpress.Domain.Audit.Interfaces;
using DtExpress.Domain.Audit.Models;
using DtExpress.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DtExpress.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAuditSink"/> and <see cref="IAuditQueryService"/>.
/// Append-only writes to the <c>audit_logs</c> table. JSONB payload serialization.
/// </summary>
public sealed class EfAuditStore : IAuditSink, IAuditQueryService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public EfAuditStore(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ── IAuditSink ──────────────────────────────────────────────

    /// <inheritdoc />
    public async Task AppendAsync(AuditRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var entity = new AuditLogEntity
        {
            Id = Guid.TryParse(record.Id, out var id) ? id : Guid.NewGuid(),
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            Action = record.Action.ToString(),
            Category = record.Category.ToString(),
            ActorUserId = _currentUser.UserId,  // Stamped from JWT claims
            ActorName = record.Actor,
            CorrelationId = record.CorrelationId,
            Timestamp = record.Timestamp,
            Description = record.Description,
            Payload = record.Payload is not null
                ? JsonSerializer.Serialize(record.Payload, JsonOptions)
                : null,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.AuditLogs.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    // ── IAuditQueryService ──────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditRecord>> GetByEntityAsync(
        string entityType, string entityId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        var entities = await _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync(ct);

        return entities.Select(MapToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditRecord>> GetByCorrelationAsync(
        string correlationId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        var entities = await _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.CorrelationId == correlationId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync(ct);

        return entities.Select(MapToDomain).ToList();
    }

    // ── Entity → Domain mapping ─────────────────────────────────

    private static AuditRecord MapToDomain(AuditLogEntity e)
    {
        Dictionary<string, object?>? payload = e.Payload is not null
            ? JsonSerializer.Deserialize<Dictionary<string, object?>>(e.Payload, JsonOptions)
            : null;

        return new AuditRecord(
            Id: e.Id.ToString(),
            EntityType: e.EntityType,
            EntityId: e.EntityId,
            Action: Enum.Parse<AuditAction>(e.Action),
            Category: Enum.Parse<AuditCategory>(e.Category),
            Actor: e.ActorName,
            CorrelationId: e.CorrelationId,
            Timestamp: e.Timestamp,
            Description: e.Description,
            Payload: payload);
    }
}
