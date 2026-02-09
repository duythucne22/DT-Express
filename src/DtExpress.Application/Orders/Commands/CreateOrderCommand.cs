using DtExpress.Application.Common;
using DtExpress.Domain.Orders.Models;
using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Application.Orders.Commands;

/// <summary>
/// CQRS Command: Create a new order.
/// Returns the new order's ID.
/// </summary>
public sealed record CreateOrderCommand(
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    Address Origin,
    Address Destination,
    ServiceLevel ServiceLevel,
    IReadOnlyList<OrderItem> Items) : ICommand<Guid>;
