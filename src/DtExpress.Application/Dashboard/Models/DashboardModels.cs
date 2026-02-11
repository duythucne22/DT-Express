namespace DtExpress.Application.Dashboard.Models;

/// <summary>Dashboard KPI aggregation result.</summary>
public sealed record DashboardStats(
    int TotalOrders,
    int TodayShipments,
    MoneyResult MonthRevenue,
    IReadOnlyList<CarrierDistribution> CarrierDistribution,
    OrderStatusBreakdown StatusBreakdown);

/// <summary>Simple money result for API responses.</summary>
public sealed record MoneyResult(decimal Amount, string Currency);

/// <summary>Carrier share in the total order volume.</summary>
public sealed record CarrierDistribution(string CarrierCode, decimal Percentage, int Count);

/// <summary>Count of orders in each status.</summary>
public sealed record OrderStatusBreakdown(
    int Created, int Confirmed, int Shipped, int Delivered, int Cancelled);

/// <summary>Carrier performance metrics.</summary>
public sealed record CarrierPerformanceResult(
    string CarrierCode,
    string CarrierName,
    int TotalBookings,
    decimal AverageCostAmount,
    string AverageCostCurrency,
    int AverageDeliveryDays,
    decimal OnTimeRate);

/// <summary>Top customer ranking result.</summary>
public sealed record TopCustomerResult(
    string CustomerName,
    string CustomerPhone,
    int OrderCount,
    decimal TotalRevenueAmount,
    string Currency,
    DateTimeOffset LastOrderDate);
