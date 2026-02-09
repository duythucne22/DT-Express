using DtExpress.Domain.Orders.Models;

namespace DtExpress.Domain.Orders.Interfaces;

/// <summary>Write-side persistence for the Order aggregate.</summary>
public interface IOrderRepository
{
    /// <summary>Load an order by ID. Returns null if not found.</summary>
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>Save (upsert) an order aggregate.</summary>
    Task SaveAsync(Order order, CancellationToken ct = default);
}
