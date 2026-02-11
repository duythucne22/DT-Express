using System.Globalization;
using System.Text;
using DtExpress.Api.Models;
using DtExpress.Application.Reports;
using DtExpress.Application.Reports.Models;
using DtExpress.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DtExpress.Api.Controllers;

/// <summary>
/// Reporting and data export: monthly shipment reports and revenue analytics.
/// Supports JSON and CSV output formats.
///
/// Shows: CQRS read-side reporting, data export, financial analytics.
/// </summary>
[ApiController]
[Route("api/reports")]
[Produces("application/json")]
[Tags("Reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportReadService _reports;
    private readonly ICorrelationIdProvider _correlationId;

    public ReportsController(IReportReadService reports, ICorrelationIdProvider correlationId)
    {
        _reports = reports;
        _correlationId = correlationId;
    }

    /// <summary>Get monthly shipment report.</summary>
    /// <remarks>
    /// Returns all shipments for a given month with order details, carrier info, and cost.
    /// Supports both **JSON** and **CSV** format via the <c>format</c> query parameter.
    ///
    /// **JSON** (default): Returns `ApiResponse&lt;MonthlyShipmentReport&gt;`
    /// **CSV**: Returns a downloadable CSV file with Content-Disposition header.
    ///
    /// **Month format**: <c>YYYY-MM</c> (e.g. <c>2026-01</c>)
    /// </remarks>
    /// <param name="month">Month in YYYY-MM format (e.g. "2026-01").</param>
    /// <param name="format">Output format: "json" (default) or "csv".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Monthly shipment report.</response>
    /// <response code="400">Invalid month format.</response>
    [HttpGet("shipments/monthly")]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(ApiResponse<MonthlyShipmentReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonthlyShipments(
        [FromQuery] string month,
        [FromQuery] string format = "json",
        CancellationToken ct = default)
    {
        // Parse month (YYYY-MM)
        if (string.IsNullOrWhiteSpace(month) ||
            !DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "VALIDATION_ERROR",
                "Invalid month format. Expected: YYYY-MM (e.g. 2026-01).",
                _correlationId.GetCorrelationId()));
        }

        var report = await _reports.GetMonthlyShipmentsAsync(parsed.Year, parsed.Month, ct);

        // CSV format
        if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = BuildShipmentCsv(report);
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"shipments-{month}.csv");
        }

        // JSON format
        var response = new MonthlyShipmentReportResponse
        {
            Year = report.Year,
            Month = report.Month,
            TotalShipments = report.TotalShipments,
            TotalRevenue = report.TotalRevenue,
            Currency = report.Currency,
            Shipments = report.Shipments.Select(s => new ShipmentReportItemResponse
            {
                OrderId = s.OrderId,
                OrderNumber = s.OrderNumber,
                CustomerName = s.CustomerName,
                Origin = s.Origin,
                Destination = s.Destination,
                Status = s.Status,
                ServiceLevel = s.ServiceLevel,
                CarrierCode = s.CarrierCode,
                TrackingNumber = s.TrackingNumber,
                Cost = s.Cost,
                CostCurrency = s.CostCurrency,
                CreatedAt = s.CreatedAt
            }).ToList()
        };

        return Ok(ApiResponse<MonthlyShipmentReportResponse>.Ok(response, _correlationId.GetCorrelationId()));
    }

    /// <summary>Get revenue breakdown by carrier.</summary>
    /// <remarks>
    /// Returns revenue analytics grouped by carrier for a date range.
    /// Includes total revenue, order count, average order value, and percentage of total.
    ///
    /// **Date format**: ISO 8601 date (e.g. <c>2026-01-01</c>)
    /// </remarks>
    /// <param name="from">Start date (inclusive), ISO 8601.</param>
    /// <param name="to">End date (exclusive), ISO 8601.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Revenue by carrier breakdown.</response>
    /// <response code="400">Invalid date format.</response>
    [HttpGet("revenue/by-carrier")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<RevenueByCarrierReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRevenueByCarrier(
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken ct = default)
    {
        if (!DateTimeOffset.TryParse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var fromDate))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "VALIDATION_ERROR",
                "Invalid 'from' date. Expected ISO 8601 format (e.g. 2026-01-01).",
                _correlationId.GetCorrelationId()));
        }

        if (!DateTimeOffset.TryParse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var toDate))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "VALIDATION_ERROR",
                "Invalid 'to' date. Expected ISO 8601 format (e.g. 2026-01-31).",
                _correlationId.GetCorrelationId()));
        }

        var report = await _reports.GetRevenueByCarrierAsync(fromDate, toDate, ct);

        var response = new RevenueByCarrierReportResponse
        {
            FromDate = report.FromDate,
            ToDate = report.ToDate,
            GrandTotal = report.GrandTotal,
            Currency = report.Currency,
            Carriers = report.Carriers.Select(c => new CarrierRevenueItemResponse
            {
                CarrierCode = c.CarrierCode,
                CarrierName = c.CarrierName,
                OrderCount = c.OrderCount,
                TotalRevenue = c.TotalRevenue,
                AverageOrderValue = c.AverageOrderValue,
                PercentageOfTotal = c.PercentageOfTotal
            }).ToList()
        };

        return Ok(ApiResponse<RevenueByCarrierReportResponse>.Ok(response, _correlationId.GetCorrelationId()));
    }

    // ── CSV Builder ──────────────────────────────────────────────

    private static string BuildShipmentCsv(MonthlyShipmentReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("OrderId,OrderNumber,CustomerName,Origin,Destination,Status,ServiceLevel,CarrierCode,TrackingNumber,Cost,Currency,CreatedAt");

        foreach (var s in report.Shipments)
        {
            sb.AppendLine(string.Join(",",
                s.OrderId,
                CsvEscape(s.OrderNumber),
                CsvEscape(s.CustomerName),
                CsvEscape(s.Origin),
                CsvEscape(s.Destination),
                s.Status,
                s.ServiceLevel,
                s.CarrierCode ?? "",
                s.TrackingNumber ?? "",
                s.Cost?.ToString("F2") ?? "",
                s.CostCurrency ?? "",
                s.CreatedAt.ToString("o")));
        }

        return sb.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

// ─────────────────────────────────────────────────────────────────
//  Response DTOs
// ─────────────────────────────────────────────────────────────────

/// <summary>Monthly shipment report response.</summary>
public sealed record MonthlyShipmentReportResponse
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int TotalShipments { get; init; }
    public decimal TotalRevenue { get; init; }
    public string Currency { get; init; } = null!;
    public IReadOnlyList<ShipmentReportItemResponse> Shipments { get; init; } = [];
}

/// <summary>Single shipment in the monthly report.</summary>
public sealed record ShipmentReportItemResponse
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string CustomerName { get; init; } = null!;
    public string Origin { get; init; } = null!;
    public string Destination { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string ServiceLevel { get; init; } = null!;
    public string? CarrierCode { get; init; }
    public string? TrackingNumber { get; init; }
    public decimal? Cost { get; init; }
    public string? CostCurrency { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>Revenue by carrier report response.</summary>
public sealed record RevenueByCarrierReportResponse
{
    public DateTimeOffset FromDate { get; init; }
    public DateTimeOffset ToDate { get; init; }
    public decimal GrandTotal { get; init; }
    public string Currency { get; init; } = null!;
    public IReadOnlyList<CarrierRevenueItemResponse> Carriers { get; init; } = [];
}

/// <summary>Revenue data for a single carrier.</summary>
public sealed record CarrierRevenueItemResponse
{
    public string CarrierCode { get; init; } = null!;
    public string CarrierName { get; init; } = null!;
    public int OrderCount { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AverageOrderValue { get; init; }
    public decimal PercentageOfTotal { get; init; }
}
