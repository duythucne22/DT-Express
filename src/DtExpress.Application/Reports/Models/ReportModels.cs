namespace DtExpress.Application.Reports.Models;

// ─────────────────────────────────────────────────────────────────
//  Monthly Shipment Report
// ─────────────────────────────────────────────────────────────────

/// <summary>Monthly shipment report with summary and per-order detail.</summary>
public sealed record MonthlyShipmentReport(
    int Year,
    int Month,
    int TotalShipments,
    decimal TotalRevenue,
    string Currency,
    IReadOnlyList<ShipmentReportItem> Shipments);

/// <summary>Single shipment line item in the monthly report.</summary>
public sealed record ShipmentReportItem(
    Guid OrderId,
    string OrderNumber,
    string CustomerName,
    string Origin,
    string Destination,
    string Status,
    string ServiceLevel,
    string? CarrierCode,
    string? TrackingNumber,
    decimal? Cost,
    string? CostCurrency,
    DateTimeOffset CreatedAt);

// ─────────────────────────────────────────────────────────────────
//  Revenue by Carrier Report
// ─────────────────────────────────────────────────────────────────

/// <summary>Revenue breakdown by carrier for a date range.</summary>
public sealed record RevenueByCarrierReport(
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    decimal GrandTotal,
    string Currency,
    IReadOnlyList<CarrierRevenueItem> Carriers);

/// <summary>Revenue data for a single carrier.</summary>
public sealed record CarrierRevenueItem(
    string CarrierCode,
    string CarrierName,
    int OrderCount,
    decimal TotalRevenue,
    decimal AverageOrderValue,
    decimal PercentageOfTotal);
