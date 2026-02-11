using DtExpress.Application.Common;
using DtExpress.Application.Orders.Commands;
using DtExpress.Application.Ports;
using DtExpress.Domain.Audit.Enums;
using DtExpress.Domain.Audit.Models;
using DtExpress.Domain.Common;
using DtExpress.Domain.Orders.Interfaces;

namespace DtExpress.Application.Orders.Handlers;

/// <summary>
/// Handles UpdateDestinationCommand: loads order, updates destination address,
/// persists changes, and records audit.
/// Only valid for orders in Created or Confirmed state.
/// </summary>
public sealed class UpdateDestinationHandler : ICommandHandler<UpdateDestinationCommand, bool>
{
    private readonly IOrderRepository _repository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IAuditPort _audit;
    private readonly IClock _clock;

    public UpdateDestinationHandler(
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

    public async Task<bool> HandleAsync(UpdateDestinationCommand command, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(command.OrderId, ct)
            ?? throw new DomainException("NOT_FOUND", $"Order {command.OrderId} not found.");

        var previousDest = order.Destination.ToShortString();

        // Domain method enforces state invariant (only Created/Confirmed)
        order.UpdateDestination(command.NewDestination, _clock.UtcNow);

        await _repository.SaveAsync(order, ct);

        // Publish domain events (if any)
        foreach (var evt in order.DomainEvents)
            await _eventPublisher.PublishAsync(evt, ct);
        order.ClearDomainEvents();

        // Audit
        await _audit.RecordAsync(new AuditContext(
            EntityType: "Order",
            EntityId: command.OrderId.ToString(),
            Action: AuditAction.BusinessAction,
            Category: AuditCategory.DataChange,
            Description: $"Destination updated: {previousDest} → {command.NewDestination.ToShortString()}"), ct);

        return true;
    }
}
