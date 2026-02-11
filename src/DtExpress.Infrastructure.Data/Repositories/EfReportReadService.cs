using DtExpress.Application.Reports;
using DtExpress.Application.Reports.Models;
using Microsoft.EntityFrameworkCore;

namespace DtExpress.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IReportReadService"/>.
/// Performs aggregation queries directly against PostgreSQL for reporting.
/// </summary>
public sealed class EfReportReadService : IReportReadService
{
    private readonly AppDbContext _db;

    public EfReportReadService(AppDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<MonthlyShipmentReport> GetMonthlyShipmentsAsync(int year, int month, CancellationToken ct = default)
    {
        var monthStart = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var monthEnd = monthStart.AddMonths(1);

        // Get orders created in the month
        var orders = await _db.Orders
            .Where(o => o.CreatedAt >= monthStart && o.CreatedAt < monthEnd)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        var orderIds = orders.Select(o => o.Id).ToList();

        // Get carrier quotes for these orders (for cost data)
        var quotes = await _db.CarrierQuotes
            .Where(q => q.OrderId.HasValue && orderIds.Contains(q.OrderId.Value))
            .ToListAsync(ct);

        var quoteLookup = quotes
            .GroupBy(q => q.OrderId!.Value)
            .ToDictionary(g => g.Key, g => g.First()); // Take cheapest/first quote per order

        var shipments = orders.Select(o =>
        {
            quoteLookup.TryGetValue(o.Id, out var quote);
            return new ShipmentReportItem(
                OrderId: o.Id,
                OrderNumber: o.OrderNumber,
                CustomerName: o.CustomerName,
                Origin: $"{o.OriginCity}, {o.OriginProvince}",
                Destination: $"{o.DestCity}, {o.DestProvince}",
                Status: o.Status,
                ServiceLevel: o.ServiceLevel,
                CarrierCode: o.CarrierCode,
                TrackingNumber: o.TrackingNumber,
                Cost: quote?.PriceAmount,
                CostCurrency: quote?.PriceCurrency,
                CreatedAt: o.CreatedAt);
        }).ToList();

        var totalRevenue = quotes.Sum(q => q.PriceAmount);

        return new MonthlyShipmentReport(
            Year: year,
            Month: month,
            TotalShipments: orders.Count,
            TotalRevenue: totalRevenue,
            Currency: "CNY",
            Shipments: shipments);
    }

    /// <inheritdoc />
    public async Task<RevenueByCarrierReport> GetRevenueByCarrierAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        // Get all carrier quotes in the date range
        var quotes = await _db.CarrierQuotes
            .Where(q => q.QuotedAt >= from && q.QuotedAt < to)
            .ToListAsync(ct);

        // Get carrier names
        var carriers = await _db.Carriers
            .ToDictionaryAsync(c => c.Code, c => c.Name, ct);

        var grandTotal = quotes.Sum(q => q.PriceAmount);

        var carrierGroups = quotes
            .GroupBy(q => q.CarrierCode)
            .Select(g =>
            {
                var totalRev = g.Sum(q => q.PriceAmount);
                var count = g.Select(q => q.OrderId).Distinct().Count();
                return new CarrierRevenueItem(
                    CarrierCode: g.Key,
                    CarrierName: carriers.TryGetValue(g.Key, out var name) ? name : g.Key,
                    OrderCount: count,
                    TotalRevenue: totalRev,
                    AverageOrderValue: count > 0 ? Math.Round(totalRev / count, 2) : 0,
                    PercentageOfTotal: grandTotal > 0
                        ? Math.Round(totalRev / grandTotal * 100, 1)
                        : 0);
            })
            .OrderByDescending(c => c.TotalRevenue)
            .ToList();

        return new RevenueByCarrierReport(
            FromDate: from,
            ToDate: to,
            GrandTotal: grandTotal,
            Currency: "CNY",
            Carriers: carrierGroups);
    }
}
