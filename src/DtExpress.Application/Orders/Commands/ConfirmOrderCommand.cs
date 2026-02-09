using DtExpress.Application.Common;

namespace DtExpress.Application.Orders.Commands;

/// <summary>
/// CQRS Command: Confirm an order (Created → Confirmed).
/// Returns true if the transition succeeded.
/// </summary>
public sealed record ConfirmOrderCommand(Guid OrderId) : ICommand<bool>;
