using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Models;

namespace DtExpress.Domain.Orders.Interfaces;

/// <summary>
/// State Pattern: each concrete state defines valid transitions.
/// Context (Order) delegates all actions to current state.
/// Invalid transitions throw DomainException.
/// </summary>
public interface IOrderState
{
    /// <summary>The status this state represents.</summary>
    OrderStatus Status { get; }

    /// <summary>
    /// Attempt to transition the order based on an action.
    /// Returns the new state if valid, throws DomainException if invalid.
    /// </summary>
    IOrderState Transition(OrderAction action, Order context);

    /// <summary>Check if this state can handle the given action (for UI enablement).</summary>
    bool CanHandle(OrderAction action);
}
