using DtExpress.Application.Common;

namespace DtExpress.Application.Orders.Commands;

/// <summary>
/// CQRS Command: Mark an order as delivered (Shipped → Delivered).
/// Returns true if the transition succeeded.
/// </summary>
public sealed record DeliverOrderCommand(Guid OrderId) : ICommand<bool>;
