using DtExpress.Domain.Audit.Interfaces;
using DtExpress.Domain.Audit.Models;
using DtExpress.Domain.Common;

namespace DtExpress.Infrastructure.Audit.Interceptors;

/// <summary>
/// Interceptor Pattern: converts an <see cref="AuditContext"/> (domain-boundary change)
/// into one or more <see cref="AuditRecord"/>s ready for the <see cref="IAuditSink"/>.
/// <para>
/// Stamps infrastructure metadata: unique ID, correlation ID, timestamp, and actor.
/// Merges Before/After dictionaries into a single Payload dictionary.
/// </para>
/// </summary>
public sealed class DomainEventAuditInterceptor : IAuditInterceptor
{
    private readonly IIdGenerator _idGenerator;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly IClock _clock;

    /// <summary>Default actor when no authenticated user context is available.</summary>
    private const string SystemActor = "system";

    public DomainEventAuditInterceptor(
        IIdGenerator idGenerator,
        ICorrelationIdProvider correlationIdProvider,
        IClock clock)
    {
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    public IReadOnlyList<AuditRecord> CaptureChanges(AuditContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var record = new AuditRecord(
            Id: _idGenerator.NewId().ToString(),
            EntityType: context.EntityType,
            EntityId: context.EntityId,
            Action: context.Action,
            Category: context.Category,
            Actor: SystemActor,
            CorrelationId: _correlationIdProvider.GetCorrelationId(),
            Timestamp: _clock.UtcNow,
            Description: context.Description,
            Payload: MergePayload(context.Before, context.After));

        return [record];
    }

    /// <summary>
    /// Merge Before/After dictionaries into a single Payload dictionary.
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
