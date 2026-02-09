using DtExpress.Application.Common;

namespace DtExpress.Application.Orders.Commands;

/// <summary>
/// CQRS Command: Cancel an order (Created or Confirmed → Cancelled).
/// Returns true if the transition succeeded.
/// </summary>
public sealed record CancelOrderCommand(Guid OrderId, string? Reason = null) : ICommand<bool>;
