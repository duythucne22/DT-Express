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
/// Handles BulkCreateOrdersCommand: validates all orders up-front,
/// then creates them in sequence. Returns per-order results.
/// Business rule: validate all before processing any.
/// </summary>
public sealed class BulkCreateOrdersHandler : ICommandHandler<BulkCreateOrdersCommand, BulkCreateResult>
{
    private readonly IOrderRepository _repository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IAuditPort _audit;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly Func<OrderStatus, IOrderState> _stateFactory;

    public BulkCreateOrdersHandler(
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

    public async Task<BulkCreateResult> HandleAsync(BulkCreateOrdersCommand command, CancellationToken ct = default)
    {
        if (command.Orders == null || command.Orders.Count == 0)
            throw new DomainException("VALIDATION_ERROR", "At least one order is required.");

        if (command.Orders.Count > 50)
            throw new DomainException("VALIDATION_ERROR", "Maximum 50 orders per bulk request.");

        // Phase 1: Validate all orders before processing any
        var validationErrors = new List<(int Index, string Error)>();
        for (int i = 0; i < command.Orders.Count; i++)
        {
            var order = command.Orders[i];
            if (string.IsNullOrWhiteSpace(order.CustomerName))
                validationErrors.Add((i, "Customer name is required."));
            if (string.IsNullOrWhiteSpace(order.CustomerPhone))
                validationErrors.Add((i, "Customer phone is required."));
            if (order.Items == null || order.Items.Count == 0)
                validationErrors.Add((i, "At least one item is required."));
        }

        if (validationErrors.Count > 0)
        {
            // Return results with validation errors (no orders created)
            var failResults = new List<BulkCreateItemResult>();
            for (int i = 0; i < command.Orders.Count; i++)
            {
                var error = validationErrors.FirstOrDefault(e => e.Index == i);
                failResults.Add(new BulkCreateItemResult(
                    Index: i,
                    Success: error == default,
                    OrderId: null,
                    OrderNumber: null,
                    Error: error != default ? error.Error : null));
            }
            return new BulkCreateResult(failResults,
                failResults.Count(r => r.Success),
                failResults.Count(r => !r.Success));
        }

        // Phase 2: Create all orders
        var now = _clock.UtcNow;
        var results = new List<BulkCreateItemResult>();

        for (int i = 0; i < command.Orders.Count; i++)
        {
            var item = command.Orders[i];
            try
            {
                var orderId = _idGenerator.NewId();
                var orderNumber = $"DT-{now:yyyyMMdd}-{orderId.ToString("N")[..6].ToUpper()}";
                var customer = new ContactInfo(item.CustomerName, item.CustomerPhone, item.CustomerEmail);

                var order = new Order(
                    id: orderId,
                    orderNumber: orderNumber,
                    customer: customer,
                    origin: item.Origin,
                    destination: item.Destination,
                    items: item.Items,
                    serviceLevel: item.ServiceLevel,
                    initialState: _stateFactory(OrderStatus.Created),
                    createdAt: now);

                await _repository.SaveAsync(order, ct);

                foreach (var evt in order.DomainEvents)
                    await _eventPublisher.PublishAsync(evt, ct);
                order.ClearDomainEvents();

                results.Add(new BulkCreateItemResult(i, true, orderId, orderNumber, null));
            }
            catch (Exception ex)
            {
                results.Add(new BulkCreateItemResult(i, false, null, null, ex.Message));
            }
        }

        // Audit the bulk operation
        await _audit.RecordAsync(new AuditContext(
            EntityType: "Order",
            EntityId: "bulk",
            Action: AuditAction.Created,
            Category: AuditCategory.DataChange,
            Description: $"Bulk created {results.Count(r => r.Success)}/{command.Orders.Count} orders"), ct);

        return new BulkCreateResult(results,
            results.Count(r => r.Success),
            results.Count(r => !r.Success));
    }
}
