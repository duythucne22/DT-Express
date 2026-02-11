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
/// Handles SplitShipmentCommand: splits an existing order into multiple orders
/// based on item groupings. The original order is cancelled and new orders
/// are created for each group.
/// Only valid for orders in Created or Confirmed state.
/// </summary>
public sealed class SplitShipmentHandler : ICommandHandler<SplitShipmentCommand, SplitShipmentResult>
{
    private readonly IOrderRepository _repository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IAuditPort _audit;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly Func<OrderStatus, IOrderState> _stateFactory;

    public SplitShipmentHandler(
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

    public async Task<SplitShipmentResult> HandleAsync(SplitShipmentCommand command, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(command.OrderId, ct)
            ?? throw new DomainException("NOT_FOUND", $"Order {command.OrderId} not found.");

        // Validate state: only Created or Confirmed can be split
        if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new DomainException("INVALID_OPERATION",
                $"Cannot split order in {order.Status} state. Only Created or Confirmed orders can be split.");

        // Validate groups
        if (command.ItemGroups == null || command.ItemGroups.Count < 2)
            throw new DomainException("VALIDATION_ERROR", "At least 2 groups are required for split.");

        var allIndices = command.ItemGroups.SelectMany(g => g).ToList();
        if (allIndices.Distinct().Count() != allIndices.Count)
            throw new DomainException("VALIDATION_ERROR", "Item indices must not overlap between groups.");

        if (allIndices.Any(i => i < 0 || i >= order.Items.Count))
            throw new DomainException("VALIDATION_ERROR",
                $"Item indices must be between 0 and {order.Items.Count - 1}.");

        // All original items must be accounted for
        if (allIndices.Count != order.Items.Count)
            throw new DomainException("VALIDATION_ERROR",
                $"All {order.Items.Count} items must be assigned to a group. Got {allIndices.Count} assignments.");

        var now = _clock.UtcNow;
        var newOrders = new List<SplitOrderInfo>();

        // Create new orders for each group
        foreach (var group in command.ItemGroups)
        {
            var items = group.Select(i => order.Items[i]).ToList();
            var newId = _idGenerator.NewId();
            var newNumber = $"DT-{now:yyyyMMdd}-{newId.ToString("N")[..6].ToUpper()}";

            var newOrder = new Order(
                id: newId,
                orderNumber: newNumber,
                customer: order.Customer,
                origin: order.Origin,
                destination: order.Destination,
                items: items,
                serviceLevel: order.ServiceLevel,
                initialState: _stateFactory(OrderStatus.Created),
                createdAt: now);

            await _repository.SaveAsync(newOrder, ct);

            foreach (var evt in newOrder.DomainEvents)
                await _eventPublisher.PublishAsync(evt, ct);
            newOrder.ClearDomainEvents();

            newOrders.Add(new SplitOrderInfo(newId, newNumber, items.Count));
        }

        // Cancel the original order
        order.Apply(OrderAction.Cancel, now);
        await _repository.SaveAsync(order, ct);

        foreach (var evt in order.DomainEvents)
            await _eventPublisher.PublishAsync(evt, ct);
        order.ClearDomainEvents();

        // Audit
        await _audit.RecordAsync(new AuditContext(
            EntityType: "Order",
            EntityId: command.OrderId.ToString(),
            Action: AuditAction.BusinessAction,
            Category: AuditCategory.DataChange,
            Description: $"Order {order.OrderNumber} split into {newOrders.Count} orders: " +
                         string.Join(", ", newOrders.Select(o => o.OrderNumber))), ct);

        return new SplitShipmentResult(command.OrderId, newOrders);
    }
}
