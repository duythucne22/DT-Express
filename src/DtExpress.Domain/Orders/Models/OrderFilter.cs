using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Routing.Enums;

namespace DtExpress.Domain.Orders.Models;

/// <summary>
/// Filter criteria for listing orders (CQRS read side).
/// All parameters optional — null means "no filter on this field".
/// </summary>
public sealed record OrderFilter(
    OrderStatus? Status = null,
    ServiceLevel? ServiceLevel = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
