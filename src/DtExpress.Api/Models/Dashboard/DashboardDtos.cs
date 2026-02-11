namespace DtExpress.Api.Models.Dashboard;

// ─────────────────────────────────────────────────────────────────
//  GET /api/dashboard/stats
// ─────────────────────────────────────────────────────────────────

/// <summary>Dashboard KPI statistics.</summary>
public sealed record DashboardStatsResponse
{
    /// <summary>Total orders across all statuses.</summary>
    public int TotalOrders { get; init; }

    /// <summary>Orders shipped today.</summary>
    public int TodayShipments { get; init; }

    /// <summary>Revenue for the current month.</summary>
    public MoneyDto MonthRevenue { get; init; } = null!;

    /// <summary>Distribution of bookings per carrier.</summary>
    public IReadOnlyList<CarrierDistributionDto> CarrierDistribution { get; init; } = [];

    /// <summary>Count of orders in each status.</summary>
    public OrderStatusBreakdownDto StatusBreakdown { get; init; } = null!;
}

/// <summary>Money amount with currency.</summary>
public sealed record MoneyDto(decimal Amount, string Currency);

/// <summary>Carrier share percentage and count.</summary>
public sealed record CarrierDistributionDto(string CarrierCode, decimal Percentage, int Count);

/// <summary>Order count breakdown by status.</summary>
public sealed record OrderStatusBreakdownDto(
    int Created, int Confirmed, int Shipped, int Delivered, int Cancelled);

// ─────────────────────────────────────────────────────────────────
//  GET /api/dashboard/carrier-performance
// ─────────────────────────────────────────────────────────────────

/// <summary>Carrier performance metrics.</summary>
public sealed record CarrierPerformanceResponse
{
    /// <summary>Carrier code (SF, JD).</summary>
    public string CarrierCode { get; init; } = null!;

    /// <summary>Carrier display name.</summary>
    public string CarrierName { get; init; } = null!;

    /// <summary>Total confirmed bookings.</summary>
    public int TotalBookings { get; init; }

    /// <summary>Average shipping cost.</summary>
    public MoneyDto AverageCost { get; init; } = null!;

    /// <summary>Average estimated delivery time in days.</summary>
    public int AverageDeliveryDays { get; init; }

    /// <summary>On-time delivery rate (percentage).</summary>
    public decimal OnTimeRate { get; init; }
}

// ─────────────────────────────────────────────────────────────────
//  GET /api/dashboard/top-customers
// ─────────────────────────────────────────────────────────────────

/// <summary>Top customer by order volume.</summary>
public sealed record TopCustomerResponse
{
    /// <summary>Customer full name.</summary>
    public string CustomerName { get; init; } = null!;

    /// <summary>Customer phone number.</summary>
    public string CustomerPhone { get; init; } = null!;

    /// <summary>Number of orders placed.</summary>
    public int OrderCount { get; init; }

    /// <summary>Total revenue generated.</summary>
    public MoneyDto TotalRevenue { get; init; } = null!;

    /// <summary>Date of most recent order.</summary>
    public DateTimeOffset LastOrderDate { get; init; }
}
