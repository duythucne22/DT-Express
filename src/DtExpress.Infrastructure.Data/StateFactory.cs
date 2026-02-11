using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Infrastructure.Orders.States;

namespace DtExpress.Infrastructure.Data;

/// <summary>
/// Maps <see cref="OrderStatus"/> enum back to the correct <see cref="IOrderState"/>
/// implementation. Used when rehydrating Order aggregates from the database.
/// States are pure-logic objects — no DI dependencies needed.
/// </summary>
internal static class StateFactory
{
    /// <summary>
    /// Create the <see cref="IOrderState"/> for the given status.
    /// </summary>
    internal static IOrderState Create(OrderStatus status) => status switch
    {
        OrderStatus.Created   => new CreatedState(),
        OrderStatus.Confirmed => new ConfirmedState(),
        OrderStatus.Shipped   => new ShippedState(),
        OrderStatus.Delivered => new DeliveredState(),
        OrderStatus.Cancelled => new CancelledState(),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, $"Unknown order status: {status}")
    };
}
