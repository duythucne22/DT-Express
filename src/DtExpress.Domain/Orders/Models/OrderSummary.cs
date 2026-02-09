using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Routing.Enums;

namespace DtExpress.Domain.Orders.Models;

/// <summary>
/// Read model: compact order summary for list views (CQRS read side).
/// </summary>
public sealed record OrderSummary(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    OrderStatus Status,
    ServiceLevel ServiceLevel,
    DateTimeOffset CreatedAt);
