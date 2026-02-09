using DtExpress.Domain.Common;
using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;

namespace DtExpress.Infrastructure.Orders.States;

/// <summary>
/// State Pattern — Delivered (已签收): terminal state, order successfully delivered.
/// <para>
/// No valid transitions. Any action throws <see cref="InvalidStateTransitionException"/>.
/// ADR-007: No reverse logistics in V1.
/// </para>
/// </summary>
public sealed class DeliveredState : IOrderState
{
    /// <inheritdoc />
    public OrderStatus Status => OrderStatus.Delivered;

    /// <inheritdoc />
    public IOrderState Transition(OrderAction action, Order context)
        => throw new InvalidStateTransitionException(Status.ToString(), action.ToString());

    /// <inheritdoc />
    public bool CanHandle(OrderAction action) => false;
}
