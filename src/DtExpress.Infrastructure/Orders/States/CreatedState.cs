using DtExpress.Domain.Common;
using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;

namespace DtExpress.Infrastructure.Orders.States;

/// <summary>
/// State Pattern — Created (已创建): initial order state.
/// <para>
/// Valid transitions:
/// <list type="bullet">
///   <item><see cref="OrderAction.Confirm"/> → <see cref="ConfirmedState"/></item>
///   <item><see cref="OrderAction.Cancel"/> → <see cref="CancelledState"/></item>
/// </list>
/// Invalid: Ship, Deliver.
/// </para>
/// </summary>
public sealed class CreatedState : IOrderState
{
    /// <inheritdoc />
    public OrderStatus Status => OrderStatus.Created;

    /// <inheritdoc />
    public IOrderState Transition(OrderAction action, Order context) => action switch
    {
        OrderAction.Confirm => new ConfirmedState(),
        OrderAction.Cancel  => new CancelledState(),
        _ => throw new InvalidStateTransitionException(Status.ToString(), action.ToString()),
    };

    /// <inheritdoc />
    public bool CanHandle(OrderAction action) => action is OrderAction.Confirm or OrderAction.Cancel;
}
