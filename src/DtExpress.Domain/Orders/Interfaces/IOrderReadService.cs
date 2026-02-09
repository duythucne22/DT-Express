using DtExpress.Domain.Orders.Models;

namespace DtExpress.Domain.Orders.Interfaces;

/// <summary>Read-side queries for CQRS. Shaped for views, not aggregates.</summary>
public interface IOrderReadService
{
    /// <summary>Get full order detail for display.</summary>
    Task<OrderDetail?> GetByIdAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>List orders with optional filtering.</summary>
    Task<IReadOnlyList<OrderSummary>> ListAsync(OrderFilter filter, CancellationToken ct = default);
}
