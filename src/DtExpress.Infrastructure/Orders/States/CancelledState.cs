using DtExpress.Domain.Common;
using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;

namespace DtExpress.Infrastructure.Orders.States;

/// <summary>
/// State Pattern — Cancelled (已取消): terminal state, order permanently cancelled.
/// <para>
/// No valid transitions. Any action throws <see cref="InvalidStateTransitionException"/>.
/// ADR-007: No reverse logistics in V1.
/// </para>
/// </summary>
public sealed class CancelledState : IOrderState
{
    /// <inheritdoc />
    public OrderStatus Status => OrderStatus.Cancelled;

    /// <inheritdoc />
    public IOrderState Transition(OrderAction action, Order context)
        => throw new InvalidStateTransitionException(Status.ToString(), action.ToString());

    /// <inheritdoc />
    public bool CanHandle(OrderAction action) => false;
}
