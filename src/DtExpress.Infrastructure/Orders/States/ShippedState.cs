using DtExpress.Domain.Common;
using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;

namespace DtExpress.Infrastructure.Orders.States;

/// <summary>
/// State Pattern — Shipped (已发货): order dispatched with carrier, tracking number assigned.
/// <para>
/// Valid transitions:
/// <list type="bullet">
///   <item><see cref="OrderAction.Deliver"/> → <see cref="DeliveredState"/></item>
/// </list>
/// Invalid: Confirm, Ship, Cancel (cannot cancel after shipping).
/// </para>
/// </summary>
public sealed class ShippedState : IOrderState
{
    /// <inheritdoc />
    public OrderStatus Status => OrderStatus.Shipped;

    /// <inheritdoc />
    public IOrderState Transition(OrderAction action, Order context) => action switch
    {
        OrderAction.Deliver => new DeliveredState(),
        _ => throw new InvalidStateTransitionException(Status.ToString(), action.ToString()),
    };

    /// <inheritdoc />
    public bool CanHandle(OrderAction action) => action is OrderAction.Deliver;
}
