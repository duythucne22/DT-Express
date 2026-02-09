using DtExpress.Application.Common;
using DtExpress.Domain.Orders.Models;

namespace DtExpress.Application.Orders.Queries;

/// <summary>
/// CQRS Query: List orders with optional filtering.
/// Returns a read-only list of order summaries.
/// </summary>
public sealed record ListOrdersQuery(OrderFilter Filter) : IQuery<IReadOnlyList<OrderSummary>>;
