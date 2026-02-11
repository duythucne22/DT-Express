using DtExpress.Application.Common;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Application.Orders.Commands;

/// <summary>
/// Update an order's destination address and trigger route recalculation.
/// Only valid for orders in Created or Confirmed state.
/// </summary>
public sealed record UpdateDestinationCommand(
    Guid OrderId,
    Address NewDestination) : ICommand<bool>;
