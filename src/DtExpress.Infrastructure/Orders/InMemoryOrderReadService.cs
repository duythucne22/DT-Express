using System.Collections.Concurrent;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;

namespace DtExpress.Infrastructure.Orders;

/// <summary>
/// In-memory read-side projection for the Order aggregate (CQRS query side).
/// Shares the same <see cref="ConcurrentDictionary{TKey,TValue}"/> backing store
/// as <see cref="InMemoryOrderRepository"/> in production wiring, but this class
/// owns its own snapshot for simplicity.
/// Projects <see cref="Order"/> → <see cref="OrderDetail"/> and <see cref="OrderSummary"/>.
/// </summary>
public sealed class InMemoryOrderReadService : IOrderReadService
{
    private readonly ConcurrentDictionary<Guid, Order> _store = new();

    /// <summary>
    /// Upsert an order into the read store.
    /// Called after the write-side persists — keeps the read projection in sync.
    /// </summary>
    public void Upsert(Order order)
        => _store.AddOrUpdate(order.Id, order, (_, _) => order);

    /// <inheritdoc />
    public Task<OrderDetail?> GetByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(orderId, out var order))
            return Task.FromResult<OrderDetail?>(null);

        return Task.FromResult<OrderDetail?>(ProjectToDetail(order));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OrderSummary>> ListAsync(OrderFilter filter, CancellationToken ct = default)
    {
        var query = _store.Values.AsEnumerable();

        if (filter.Status.HasValue)
            query = query.Where(o => o.Status == filter.Status.Value);

        if (filter.ServiceLevel.HasValue)
            query = query.Where(o => o.ServiceLevel == filter.ServiceLevel.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(o => o.CreatedAt <= filter.ToDate.Value);

        IReadOnlyList<OrderSummary> result = query
            .Select(ProjectToSummary)
            .OrderByDescending(s => s.CreatedAt)
            .ToList();

        return Task.FromResult(result);
    }

    // ── Projections ──────────────────────────────────────────────

    private static OrderDetail ProjectToDetail(Order o) => new(
        Id: o.Id,
        OrderNumber: o.OrderNumber,
        CustomerName: o.Customer.Name,
        Origin: o.Origin.ToShortString(),
        Destination: o.Destination.ToShortString(),
        Status: o.Status,
        ServiceLevel: o.ServiceLevel,
        TrackingNumber: o.TrackingNumber,
        CarrierCode: o.CarrierCode,
        Items: o.Items,
        CreatedAt: o.CreatedAt,
        UpdatedAt: o.UpdatedAt);

    private static OrderSummary ProjectToSummary(Order o) => new(
        Id: o.Id,
        OrderNumber: o.OrderNumber,
        CustomerName: o.Customer.Name,
        Status: o.Status,
        ServiceLevel: o.ServiceLevel,
        CreatedAt: o.CreatedAt);
}
