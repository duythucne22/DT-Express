using DtExpress.Application.Common;
using DtExpress.Application.Orders.Queries;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;

namespace DtExpress.Application.Orders.Handlers;

/// <summary>
/// Handles GetOrderByIdQuery: reads from IOrderReadService (CQRS read side).
/// Returns null if order not found.
/// </summary>
public sealed class GetOrderByIdHandler : IQueryHandler<GetOrderByIdQuery, OrderDetail?>
{
    private readonly IOrderReadService _readService;

    public GetOrderByIdHandler(IOrderReadService readService)
    {
        _readService = readService;
    }

    public async Task<OrderDetail?> HandleAsync(GetOrderByIdQuery query, CancellationToken ct = default)
    {
        return await _readService.GetByIdAsync(query.OrderId, ct);
    }
}
