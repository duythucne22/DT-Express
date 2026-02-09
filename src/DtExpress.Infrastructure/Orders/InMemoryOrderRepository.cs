using System.Collections.Concurrent;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;
using DtExpress.Infrastructure.Orders.States;

namespace DtExpress.Infrastructure.Orders;

/// <summary>
/// In-memory write-side store for the Order aggregate (CQRS command side).
/// Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread-safe upsert.
/// If a newly saved order has a null <see cref="Order.CurrentState"/>,
/// the repository stamps <see cref="CreatedState"/> — this decouples the
/// Application layer from knowing concrete state classes.
/// </summary>
public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _store = new();

    /// <inheritdoc />
    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        _store.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    /// <inheritdoc />
    public Task SaveAsync(Order order, CancellationToken ct = default)
    {
        _store.AddOrUpdate(order.Id, order, (_, _) => order);
        return Task.CompletedTask;
    }
}
