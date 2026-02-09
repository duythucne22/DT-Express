using DtExpress.Domain.Orders.Enums;

namespace DtExpress.Domain.Orders.Models;

/// <summary>
/// Domain event emitted when an order transitions between states.
/// Collected in the Order aggregate, published after save.
/// </summary>
public sealed record OrderDomainEvent(
    Guid OrderId,
    OrderStatus PreviousStatus,
    OrderStatus NewStatus,
    OrderAction Action,
    DateTimeOffset OccurredAt);
