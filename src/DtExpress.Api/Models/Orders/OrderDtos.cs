using System.ComponentModel.DataAnnotations;

namespace DtExpress.Api.Models.Orders;

// ─────────────────────────────────────────────────────────────────
//  Shared sub-DTOs
// ─────────────────────────────────────────────────────────────────

/// <summary>Address for order origin/destination.</summary>
public sealed record OrderAddressDto
{
    [Required] public string Street { get; init; } = null!;
    [Required] public string City { get; init; } = null!;
    [Required] public string Province { get; init; } = null!;
    [Required] public string PostalCode { get; init; } = null!;
    public string Country { get; init; } = "CN";
}

/// <summary>Weight for order items.</summary>
public sealed record OrderWeightDto(
    [property: Required, Range(0.01, double.MaxValue)] decimal Value,
    [property: Required] string Unit);

/// <summary>Dimension for order items (optional).</summary>
public sealed record OrderDimensionDto(
    [property: Required, Range(0.01, double.MaxValue)] decimal LengthCm,
    [property: Required, Range(0.01, double.MaxValue)] decimal WidthCm,
    [property: Required, Range(0.01, double.MaxValue)] decimal HeightCm);

/// <summary>Line item within an order.</summary>
public sealed record OrderItemDto
{
    [Required] public string Description { get; init; } = null!;
    [Required, Range(1, int.MaxValue)] public int Quantity { get; init; }
    [Required] public OrderWeightDto Weight { get; init; } = null!;
    public OrderDimensionDto? Dimensions { get; init; }
}

// ─────────────────────────────────────────────────────────────────
//  POST /api/orders
// ─────────────────────────────────────────────────────────────────

/// <summary>Request body for creating a new order.</summary>
public sealed record CreateOrderRequest
{
    /// <summary>Customer full name (e.g. 张三).</summary>
    [Required] public string CustomerName { get; init; } = null!;

    /// <summary>Chinese mobile phone (e.g. 13812345678).</summary>
    [Required] public string CustomerPhone { get; init; } = null!;

    /// <summary>Optional email address.</summary>
    public string? CustomerEmail { get; init; }

    /// <summary>Pickup/origin address.</summary>
    [Required] public OrderAddressDto Origin { get; init; } = null!;

    /// <summary>Delivery destination address.</summary>
    [Required] public OrderAddressDto Destination { get; init; } = null!;

    /// <summary>Delivery speed tier: Express, Standard, Economy.</summary>
    [Required] public string ServiceLevel { get; init; } = null!;

    /// <summary>Package items to ship.</summary>
    [Required, MinLength(1)] public List<OrderItemDto> Items { get; init; } = null!;
}

/// <summary>Response after order creation.</summary>
public sealed record CreateOrderResponse(Guid OrderId, string OrderNumber, string Status);

// ─────────────────────────────────────────────────────────────────
//  GET /api/orders/{id}
// ─────────────────────────────────────────────────────────────────

/// <summary>Full order detail read model.</summary>
public sealed record OrderDetailResponse
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string CustomerName { get; init; } = null!;
    public string Origin { get; init; } = null!;
    public string Destination { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string ServiceLevel { get; init; } = null!;
    public string? TrackingNumber { get; init; }
    public string? CarrierCode { get; init; }
    public IReadOnlyList<OrderItemResponse> Items { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>Order item in response.</summary>
public sealed record OrderItemResponse(
    string Description,
    int Quantity,
    OrderWeightDto Weight,
    OrderDimensionDto? Dimensions);

// ─────────────────────────────────────────────────────────────────
//  GET /api/orders (list)
// ─────────────────────────────────────────────────────────────────

/// <summary>Compact order summary for list views.</summary>
public sealed record OrderSummaryResponse(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string Status,
    string ServiceLevel,
    DateTimeOffset CreatedAt);

// ─────────────────────────────────────────────────────────────────
//  PUT /api/orders/{id}/confirm|ship|deliver|cancel
// ─────────────────────────────────────────────────────────────────

/// <summary>Response for state-transition actions.</summary>
public sealed record OrderTransitionResponse
{
    public Guid OrderId { get; init; }
    public string NewStatus { get; init; } = null!;
    public string? CarrierCode { get; init; }
    public string? TrackingNumber { get; init; }
}

/// <summary>Request body for cancel action (optional reason).</summary>
public sealed record CancelOrderRequest
{
    public string? Reason { get; init; }
}
