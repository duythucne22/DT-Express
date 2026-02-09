using DtExpress.Domain.Orders.Models;

namespace DtExpress.Application.Common;

/// <summary>Publishes domain events to interested listeners (audit, tracking, etc.).</summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(OrderDomainEvent domainEvent, CancellationToken ct = default);
}
