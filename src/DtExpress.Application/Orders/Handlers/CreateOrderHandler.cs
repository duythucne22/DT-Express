using DtExpress.Application.Common;
using DtExpress.Application.Orders.Commands;
using DtExpress.Application.Ports;
using DtExpress.Domain.Audit.Enums;
using DtExpress.Domain.Audit.Models;
using DtExpress.Domain.Common;
using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Application.Orders.Handlers;

/// <summary>
/// Handles CreateOrderCommand: validates input, creates Order aggregate,
/// persists it, publishes domain events, and records audit.
/// </summary>
public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _repository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IAuditPort _audit;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly Func<OrderStatus, IOrderState> _stateFactory;

    public CreateOrderHandler(
        IOrderRepository repository,
        IDomainEventPublisher eventPublisher,
        IAuditPort audit,
        IClock clock,
        IIdGenerator idGenerator,
        Func<OrderStatus, IOrderState> stateFactory)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _audit = audit;
        _clock = clock;
        _idGenerator = idGenerator;
        _stateFactory = stateFactory;
    }

    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken ct = default)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(command.CustomerName))
            throw new DomainException("VALIDATION_ERROR", "Customer name is required.");
        if (command.Items == null || command.Items.Count == 0)
            throw new DomainException("VALIDATION_ERROR", "At least one item is required.");

        var now = _clock.UtcNow;
        var orderId = _idGenerator.NewId();
        var orderNumber = $"DT-{now:yyyyMMdd}-{orderId.ToString("N")[..6].ToUpper()}";

        var customer = new ContactInfo(command.CustomerName, command.CustomerPhone, command.CustomerEmail);

        // Create aggregate with initial Created state
        var order = new Order(
            id: orderId,
            orderNumber: orderNumber,
            customer: customer,
            origin: command.Origin,
            destination: command.Destination,
            items: command.Items,
            serviceLevel: command.ServiceLevel,
            initialState: _stateFactory(OrderStatus.Created),
            createdAt: now);

        // Persist
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
            EntityId: orderId.ToString(),
            Action: AuditAction.Created,
            Category: AuditCategory.DataChange,
            Description: $"Order {orderNumber} created for {command.CustomerName}"), ct);

        return orderId;
    }
}
