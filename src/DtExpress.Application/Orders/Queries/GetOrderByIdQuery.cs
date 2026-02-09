using DtExpress.Application.Common;
using DtExpress.Domain.Orders.Models;

namespace DtExpress.Application.Orders.Queries;

/// <summary>
/// CQRS Query: Get full order detail by ID.
/// Returns null if order not found.
/// </summary>
public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDetail?>;
