using DtExpress.Application.Common;
using DtExpress.Application.Orders.Commands;
using DtExpress.Application.Ports;
using DtExpress.Domain.Audit.Enums;
using DtExpress.Domain.Audit.Models;
using DtExpress.Domain.Common;
using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;

namespace DtExpress.Application.Orders.Handlers;

/// <summary>
/// Handles DeliverOrderCommand: transitions Shipped → Delivered,
/// persists, publishes events, and records audit.
/// </summary>
public sealed class DeliverOrderHandler : ICommandHandler<DeliverOrderCommand, bool>
{
    private readonly IOrderRepository _repository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IAuditPort _audit;
    private readonly IClock _clock;

    public DeliverOrderHandler(
        IOrderRepository repository,
        IDomainEventPublisher eventPublisher,
        IAuditPort audit,
        IClock clock)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _audit = audit;
        _clock = clock;
    }

    public async Task<bool> HandleAsync(DeliverOrderCommand command, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(command.OrderId, ct)
            ?? throw new DomainException("NOT_FOUND", $"Order {command.OrderId} not found.");

        var previousStatus = order.Status;

        // State Pattern: delegates transition validation to CurrentState
        order.Apply(OrderAction.Deliver, _clock.UtcNow);

        await _repository.SaveAsync(order, ct);

        // Publish domain events
        foreach (var evt in order.DomainEvents)
        {
            await _eventPublisher.PublishAsync(evt, ct);
        }
        order.ClearDomainEvents();

        // Audit
        await _audit.RecordAsync(new AuditContext(
            EntityType: "Order",
            EntityId: command.OrderId.ToString(),
            Action: AuditAction.StateChanged,
            Category: AuditCategory.StateTransition,
            Description: $"State: {previousStatus} → {order.Status}"), ct);

        return true;
    }
}
