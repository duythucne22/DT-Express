using DtExpress.Application.Dashboard;
using DtExpress.Application.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace DtExpress.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDashboardReadService"/>.
/// Performs aggregation queries directly against PostgreSQL for dashboard KPIs.
/// </summary>
public sealed class EfDashboardReadService : IDashboardReadService
{
    private readonly AppDbContext _db;

    public EfDashboardReadService(AppDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<DashboardStats> GetStatsAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var today = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        // Total orders
        var totalOrders = await _db.Orders.CountAsync(ct);

        // Today's shipments (orders that entered Shipped status today)
        var todayShipments = await _db.Orders
            .Where(o => o.Status == "Shipped" && o.UpdatedAt >= today)
            .CountAsync(ct);

        // Month revenue from carrier quotes
        var monthRevenue = await _db.CarrierQuotes
            .Where(q => q.QuotedAt >= monthStart)
            .SumAsync(q => q.PriceAmount, ct);

        // Carrier distribution from bookings
        var bookingsByCarrier = await _db.Bookings
            .GroupBy(b => b.CarrierCode)
            .Select(g => new { CarrierCode = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var totalBookings = bookingsByCarrier.Sum(b => b.Count);
        var carrierDist = bookingsByCarrier.Select(b => new CarrierDistribution(
            b.CarrierCode,
            totalBookings > 0 ? Math.Round((decimal)b.Count / totalBookings * 100, 1) : 0,
            b.Count
        )).ToList();

        // Status breakdown
        var statusGroups = await _db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var breakdown = new OrderStatusBreakdown(
            Created: statusGroups.FirstOrDefault(s => s.Status == "Created")?.Count ?? 0,
            Confirmed: statusGroups.FirstOrDefault(s => s.Status == "Confirmed")?.Count ?? 0,
            Shipped: statusGroups.FirstOrDefault(s => s.Status == "Shipped")?.Count ?? 0,
            Delivered: statusGroups.FirstOrDefault(s => s.Status == "Delivered")?.Count ?? 0,
            Cancelled: statusGroups.FirstOrDefault(s => s.Status == "Cancelled")?.Count ?? 0);

        return new DashboardStats(
            TotalOrders: totalOrders,
            TodayShipments: todayShipments,
            MonthRevenue: new MoneyResult(monthRevenue, "CNY"),
            CarrierDistribution: carrierDist,
            StatusBreakdown: breakdown);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CarrierPerformanceResult>> GetCarrierPerformanceAsync(CancellationToken ct = default)
    {
        // Get all carriers
        var carriers = await _db.Carriers
            .Where(c => c.IsActive)
            .Select(c => new { c.Code, c.Name })
            .ToListAsync(ct);

        var results = new List<CarrierPerformanceResult>();

        foreach (var carrier in carriers)
        {
            // Bookings count
            var bookingCount = await _db.Bookings
                .Where(b => b.CarrierCode == carrier.Code)
                .CountAsync(ct);

            // Average cost from carrier quotes
            var avgCost = await _db.CarrierQuotes
                .Where(q => q.CarrierCode == carrier.Code)
                .Select(q => (decimal?)q.PriceAmount)
                .AverageAsync(ct) ?? 0m;

            // Average estimated delivery days
            var avgDays = await _db.CarrierQuotes
                .Where(q => q.CarrierCode == carrier.Code)
                .Select(q => (double?)q.EstimatedDays)
                .AverageAsync(ct) ?? 0;

            // On-time rate: ratio of delivered orders vs shipped orders for this carrier
            var shippedWithCarrier = await _db.Orders
                .Where(o => o.CarrierCode == carrier.Code &&
                       (o.Status == "Shipped" || o.Status == "Delivered"))
                .CountAsync(ct);

            var deliveredWithCarrier = await _db.Orders
                .Where(o => o.CarrierCode == carrier.Code && o.Status == "Delivered")
                .CountAsync(ct);

            var onTimeRate = shippedWithCarrier > 0
                ? Math.Round((decimal)deliveredWithCarrier / shippedWithCarrier * 100, 1)
                : 100m; // No data = assume 100%

            results.Add(new CarrierPerformanceResult(
                CarrierCode: carrier.Code,
                CarrierName: carrier.Name,
                TotalBookings: bookingCount,
                AverageCostAmount: Math.Round(avgCost, 2),
                AverageCostCurrency: "CNY",
                AverageDeliveryDays: (int)Math.Round(avgDays),
                OnTimeRate: onTimeRate));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TopCustomerResult>> GetTopCustomersAsync(int limit, CancellationToken ct = default)
    {
        // Group orders by customer, count and sum revenue
        var customerGroups = await _db.Orders
            .GroupBy(o => new { o.CustomerName, o.CustomerPhone })
            .Select(g => new
            {
                g.Key.CustomerName,
                g.Key.CustomerPhone,
                OrderCount = g.Count(),
                LastOrderDate = g.Max(o => o.CreatedAt)
            })
            .OrderByDescending(c => c.OrderCount)
            .Take(limit)
            .ToListAsync(ct);

        var results = new List<TopCustomerResult>();
        foreach (var cg in customerGroups)
        {
            // Get total revenue for this customer from carrier quotes
            // (join via orders → carrier_quotes)
            var customerOrderIds = await _db.Orders
                .Where(o => o.CustomerName == cg.CustomerName && o.CustomerPhone == cg.CustomerPhone)
                .Select(o => o.Id)
                .ToListAsync(ct);

            var totalRevenue = await _db.CarrierQuotes
                .Where(q => q.OrderId.HasValue && customerOrderIds.Contains(q.OrderId.Value))
                .SumAsync(q => q.PriceAmount, ct);

            results.Add(new TopCustomerResult(
                CustomerName: cg.CustomerName,
                CustomerPhone: cg.CustomerPhone,
                OrderCount: cg.OrderCount,
                TotalRevenueAmount: totalRevenue,
                Currency: "CNY",
                LastOrderDate: cg.LastOrderDate));
        }

        return results;
    }
}
