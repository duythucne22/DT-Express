using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Routing.Enums;

namespace DtExpress.Domain.Orders.Models;

/// <summary>
/// Read model: full order view for query/display (CQRS read side).
/// </summary>
public sealed record OrderDetail(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string Origin,
    string Destination,
    OrderStatus Status,
    ServiceLevel ServiceLevel,
    string? TrackingNumber,
    string? CarrierCode,
    IReadOnlyList<OrderItem> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
