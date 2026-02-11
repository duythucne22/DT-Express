using DtExpress.Application.Dashboard.Models;

namespace DtExpress.Application.Dashboard;

/// <summary>
/// Read-side service for dashboard analytics and KPI aggregation.
/// Implemented by EF Core in Infrastructure.Data.
/// </summary>
public interface IDashboardReadService
{
    /// <summary>Get aggregated KPI stats (total orders, today's shipments, revenue, carrier distribution).</summary>
    Task<DashboardStats> GetStatsAsync(CancellationToken ct = default);

    /// <summary>Get performance metrics per carrier (bookings, avg cost, on-time rate).</summary>
    Task<IReadOnlyList<CarrierPerformanceResult>> GetCarrierPerformanceAsync(CancellationToken ct = default);

    /// <summary>Get top customers by order count.</summary>
    Task<IReadOnlyList<TopCustomerResult>> GetTopCustomersAsync(int limit, CancellationToken ct = default);
}
