using DtExpress.Application.Common;
using DtExpress.Application.Orders.Queries;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;

namespace DtExpress.Application.Orders.Handlers;

/// <summary>
/// Handles ListOrdersQuery: reads from IOrderReadService with filter (CQRS read side).
/// </summary>
public sealed class ListOrdersHandler : IQueryHandler<ListOrdersQuery, IReadOnlyList<OrderSummary>>
{
    private readonly IOrderReadService _readService;

    public ListOrdersHandler(IOrderReadService readService)
    {
        _readService = readService;
    }

    public async Task<IReadOnlyList<OrderSummary>> HandleAsync(ListOrdersQuery query, CancellationToken ct = default)
    {
        return await _readService.ListAsync(query.Filter, ct);
    }
}
