using DtExpress.Application.Auth.Services;
using DtExpress.Application.Ports;
using DtExpress.Domain.Audit.Interfaces;
using DtExpress.Domain.Audit.Models;
using DtExpress.Domain.Common;

namespace DtExpress.Infrastructure.Orders.Ports;

/// <summary>
/// Port Adapter: bridges <see cref="IAuditPort"/> (Application layer) to
/// <see cref="IAuditSink"/> (Domain append-only storage).
/// <para>
/// Converts <see cref="AuditContext"/> → <see cref="AuditRecord"/> by
/// stamping identity, correlation, and timestamp via infrastructure services:
/// <see cref="IIdGenerator"/>, <see cref="ICorrelationIdProvider"/>, <see cref="IClock"/>.
/// </para>
/// </summary>
public sealed class AuditPortAdapter : IAuditPort
{
    private readonly IAuditSink _sink;
    private readonly IIdGenerator _idGenerator;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly IClock _clock;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Default actor when no authenticated user context is available.</summary>
    private const string SystemActor = "system";

    public AuditPortAdapter(
        IAuditSink sink,
        IIdGenerator idGenerator,
        ICorrelationIdProvider correlationIdProvider,
        IClock clock,
        ICurrentUserService currentUser)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <inheritdoc />
    public Task RecordAsync(AuditContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var record = new AuditRecord(
            Id: _idGenerator.NewId().ToString(),
            EntityType: context.EntityType,
            EntityId: context.EntityId,
            Action: context.Action,
            Category: context.Category,
            Actor: _currentUser.DisplayName ?? _currentUser.UserName ?? SystemActor,
            CorrelationId: _correlationIdProvider.GetCorrelationId(),
            Timestamp: _clock.UtcNow,
            Description: context.Description,
            Payload: MergePayload(context.Before, context.After));

        return _sink.AppendAsync(record, ct);
    }

    /// <summary>
    /// Merge Before/After dictionaries into a single Payload dictionary for the audit record.
    /// Returns null when both inputs are null.
    /// </summary>
    private static Dictionary<string, object?>? MergePayload(
        Dictionary<string, object?>? before,
        Dictionary<string, object?>? after)
    {
        if (before is null && after is null)
            return null;

        var payload = new Dictionary<string, object?>();

        if (before is not null)
            payload["before"] = before;

        if (after is not null)
            payload["after"] = after;

        return payload;
    }
}
