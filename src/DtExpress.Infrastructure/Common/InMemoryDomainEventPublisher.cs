using DtExpress.Application.Common;
using DtExpress.Domain.Orders.Models;
using Microsoft.Extensions.Logging;

namespace DtExpress.Infrastructure.Common;

/// <summary>
/// Implements <see cref="IDomainEventPublisher"/> with in-memory event logging.
/// Logs each domain event via <see cref="ILogger"/> and stores events for later retrieval.
/// <para>
/// In Phase 4, this will bridge to <c>IAuditInterceptor</c> and tracking observers.
/// For now (Task 3.1), it serves as the event logging backbone.
/// </para>
/// </summary>
public sealed class InMemoryDomainEventPublisher : IDomainEventPublisher
{
    private readonly ILogger<InMemoryDomainEventPublisher> _logger;
    private readonly List<OrderDomainEvent> _publishedEvents = [];
    private readonly object _lock = new();

    public InMemoryDomainEventPublisher(ILogger<InMemoryDomainEventPublisher> logger)
        => _logger = logger;

    /// <inheritdoc />
    public Task PublishAsync(OrderDomainEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _logger.LogInformation(
            "Domain event published: Order {OrderId} transitioned {PreviousStatus} → {NewStatus} via {Action} at {OccurredAt}",
            domainEvent.OrderId,
            domainEvent.PreviousStatus,
            domainEvent.NewStatus,
            domainEvent.Action,
            domainEvent.OccurredAt);

        lock (_lock)
        {
            _publishedEvents.Add(domainEvent);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a snapshot of all published events. Used for testing and audit bridging.
    /// </summary>
    public IReadOnlyList<OrderDomainEvent> GetPublishedEvents()
    {
        lock (_lock)
        {
            return _publishedEvents.ToList().AsReadOnly();
        }
    }
}
