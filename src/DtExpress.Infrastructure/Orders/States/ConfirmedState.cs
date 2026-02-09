using DtExpress.Domain.Common;
using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;

namespace DtExpress.Infrastructure.Orders.States;

/// <summary>
/// State Pattern — Confirmed (已确认): order validated, ready for carrier booking.
/// <para>
/// Valid transitions:
/// <list type="bullet">
///   <item><see cref="OrderAction.Ship"/> → <see cref="ShippedState"/></item>
///   <item><see cref="OrderAction.Cancel"/> → <see cref="CancelledState"/></item>
/// </list>
/// Invalid: Confirm, Deliver.
/// </para>
/// </summary>
public sealed class ConfirmedState : IOrderState
{
    /// <inheritdoc />
    public OrderStatus Status => OrderStatus.Confirmed;

    /// <inheritdoc />
    public IOrderState Transition(OrderAction action, Order context) => action switch
    {
        OrderAction.Ship   => new ShippedState(),
        OrderAction.Cancel => new CancelledState(),
        _ => throw new InvalidStateTransitionException(Status.ToString(), action.ToString()),
    };

    /// <inheritdoc />
    public bool CanHandle(OrderAction action) => action is OrderAction.Ship or OrderAction.Cancel;
}
