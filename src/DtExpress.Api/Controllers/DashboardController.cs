using DtExpress.Api.Models;
using DtExpress.Api.Models.Dashboard;
using DtExpress.Application.Dashboard;
using DtExpress.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DtExpress.Api.Controllers;

/// <summary>
/// Dashboard analytics: KPIs, carrier performance, and top customers.
/// Read-only aggregation queries for business intelligence.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
[Tags("Dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardReadService _dashboard;
    private readonly ICorrelationIdProvider _correlationId;

    public DashboardController(
        IDashboardReadService dashboard,
        ICorrelationIdProvider correlationId)
    {
        _dashboard = dashboard;
        _correlationId = correlationId;
    }

    /// <summary>Get dashboard KPI statistics.</summary>
    /// <remarks>
    /// Returns aggregated metrics: total orders, today's shipments, monthly revenue,
    /// carrier distribution, and order status breakdown.
    /// Data is calculated in real-time from the database.
    /// </remarks>
    /// <response code="200">Dashboard statistics.</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var stats = await _dashboard.GetStatsAsync(ct);

        var response = new DashboardStatsResponse
        {
            TotalOrders = stats.TotalOrders,
            TodayShipments = stats.TodayShipments,
            MonthRevenue = new MoneyDto(stats.MonthRevenue.Amount, stats.MonthRevenue.Currency),
            CarrierDistribution = stats.CarrierDistribution
                .Select(c => new CarrierDistributionDto(c.CarrierCode, c.Percentage, c.Count))
                .ToList(),
            StatusBreakdown = new OrderStatusBreakdownDto(
                stats.StatusBreakdown.Created,
                stats.StatusBreakdown.Confirmed,
                stats.StatusBreakdown.Shipped,
                stats.StatusBreakdown.Delivered,
                stats.StatusBreakdown.Cancelled)
        };

        return Ok(ApiResponse<DashboardStatsResponse>.Ok(response, _correlationId.GetCorrelationId()));
    }

    /// <summary>Get carrier performance metrics.</summary>
    /// <remarks>
    /// Returns per-carrier metrics: total bookings, average cost, average delivery days,
    /// and on-time delivery rate. Useful for comparing SF Express vs JD Logistics.
    /// </remarks>
    /// <response code="200">Carrier performance comparison.</response>
    [HttpGet("carrier-performance")]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CarrierPerformanceResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCarrierPerformance(CancellationToken ct)
    {
        var performance = await _dashboard.GetCarrierPerformanceAsync(ct);

        var response = performance.Select(p => new CarrierPerformanceResponse
        {
            CarrierCode = p.CarrierCode,
            CarrierName = p.CarrierName,
            TotalBookings = p.TotalBookings,
            AverageCost = new MoneyDto(p.AverageCostAmount, p.AverageCostCurrency),
            AverageDeliveryDays = p.AverageDeliveryDays,
            OnTimeRate = p.OnTimeRate
        }).ToList();

        return Ok(ApiResponse<IReadOnlyList<CarrierPerformanceResponse>>.Ok(
            response, _correlationId.GetCorrelationId()));
    }

    /// <summary>Get top customers by order volume.</summary>
    /// <remarks>
    /// Returns customers ranked by number of orders placed, including total revenue
    /// generated and last order date. Default limit is 5.
    /// </remarks>
    /// <param name="limit">Maximum number of customers to return (1-50, default: 5).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Top customers ranked by order count.</response>
    [HttpGet("top-customers")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TopCustomerResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopCustomers(
        [FromQuery] int limit = 5,
        CancellationToken ct = default)
    {
        if (limit < 1) limit = 1;
        if (limit > 50) limit = 50;

        var customers = await _dashboard.GetTopCustomersAsync(limit, ct);

        var response = customers.Select(c => new TopCustomerResponse
        {
            CustomerName = c.CustomerName,
            CustomerPhone = c.CustomerPhone,
            OrderCount = c.OrderCount,
            TotalRevenue = new MoneyDto(c.TotalRevenueAmount, c.Currency),
            LastOrderDate = c.LastOrderDate
        }).ToList();

        return Ok(ApiResponse<IReadOnlyList<TopCustomerResponse>>.Ok(
            response, _correlationId.GetCorrelationId()));
    }
}
