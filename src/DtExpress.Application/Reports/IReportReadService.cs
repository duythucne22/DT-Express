using DtExpress.Application.Reports.Models;

namespace DtExpress.Application.Reports;

/// <summary>
/// Read-side reporting service for analytics and data export.
/// Follows CQRS: this is a dedicated query port for reporting.
/// </summary>
public interface IReportReadService
{
    /// <summary>
    /// Get monthly shipment report for a given month.
    /// Returns per-order shipment data with carrier info, status, and cost.
    /// </summary>
    Task<MonthlyShipmentReport> GetMonthlyShipmentsAsync(int year, int month, CancellationToken ct = default);

    /// <summary>
    /// Get revenue breakdown grouped by carrier for a date range.
    /// Returns total revenue, order count, and average cost per carrier.
    /// </summary>
    Task<RevenueByCarrierReport> GetRevenueByCarrierAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
