using DtExpress.Application.Common;
using DtExpress.Domain.Carrier.Models;

namespace DtExpress.Application.Orders.Commands;

/// <summary>
/// CQRS Command: Ship an order (Confirmed → Shipped).
/// Triggers routing calculation + carrier booking.
/// Returns the BookingResult with tracking number.
/// </summary>
public sealed record ShipOrderCommand(Guid OrderId) : ICommand<BookingResult>;
