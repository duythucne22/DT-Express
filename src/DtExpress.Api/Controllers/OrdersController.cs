using DtExpress.Api.Models;
using DtExpress.Api.Models.Orders;
using DtExpress.Application.Common;
using DtExpress.Application.Orders.Commands;
using DtExpress.Application.Orders.Queries;
using DtExpress.Domain.Carrier.Models;
using DtExpress.Domain.Common;
using DtExpress.Domain.Orders.Models;
using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DtExpress.Api.Controllers;

/// <summary>
/// Order lifecycle management via CQRS.
/// Commands (create, confirm, ship, deliver, cancel) go through ICommandDispatcher.
/// Queries (get by ID, list) go through IQueryDispatcher.
/// </summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
[Tags("Orders")]
[Authorize]
public sealed class OrdersController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;
    private readonly ICorrelationIdProvider _correlationId;

    public OrdersController(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        ICorrelationIdProvider correlationId)
    {
        _commands = commands;
        _queries = queries;
        _correlationId = correlationId;
    }

    /// <summary>Create a new order.</summary>
    /// <remarks>
    /// Creates an order in **Created** state with customer info, addresses, service level, and items.
    /// The order number is auto-generated (format: DT-YYYYMMDD-NNN).
    /// Dispatches CreateOrderCommand → CreateOrderHandler.
    /// </remarks>
    /// <response code="201">Order created successfully.</response>
    /// <response code="400">Invalid request data (validation error).</response>
    [HttpPost]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(ApiResponse<CreateOrderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var command = new CreateOrderCommand(
            request.CustomerName,
            request.CustomerPhone,
            request.CustomerEmail,
            MapToAddress(request.Origin),
            MapToAddress(request.Destination),
            Enum.Parse<ServiceLevel>(request.ServiceLevel, ignoreCase: true),
            request.Items.Select(MapToOrderItem).ToList());

        var orderId = await _commands.DispatchAsync<Guid>(command, ct);

        // Fetch the created order to get the order number
        var detail = await _queries.DispatchAsync<OrderDetail?>(new GetOrderByIdQuery(orderId), ct);

        var response = new CreateOrderResponse(
            orderId,
            detail?.OrderNumber ?? $"DT-{DateTime.UtcNow:yyyyMMdd}",
            "Created");

        return CreatedAtAction(
            nameof(GetById),
            new { id = orderId },
            ApiResponse<CreateOrderResponse>.Ok(response, _correlationId.GetCorrelationId()));
    }

    /// <summary>Get full order detail by ID.</summary>
    /// <remarks>
    /// Returns the complete order read model including items, status, tracking info, and timestamps.
    /// Dispatches GetOrderByIdQuery → GetOrderByIdHandler.
    /// </remarks>
    /// <param name="id">Order ID (GUID).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Order detail.</response>
    /// <response code="404">Order not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _queries.DispatchAsync<OrderDetail?>(new GetOrderByIdQuery(id), ct);

        if (result is null)
            return NotFound(ApiResponse<object>.Fail(
                "NOT_FOUND", $"Order {id} not found.", _correlationId.GetCorrelationId()));

        return Ok(ApiResponse<OrderDetailResponse>.Ok(
            MapToOrderDetailResponse(result), _correlationId.GetCorrelationId()));
    }

    /// <summary>List orders with optional filters.</summary>
    /// <remarks>
    /// Supports filtering by status, service level, and date range.
    /// All parameters are optional — omitting a filter returns all matches.
    /// Dispatches ListOrdersQuery → ListOrdersHandler.
    /// </remarks>
    /// <param name="status">Filter by order status (Created, Confirmed, Shipped, Delivered, Cancelled).</param>
    /// <param name="serviceLevel">Filter by service level (Express, Standard, Economy).</param>
    /// <param name="fromDate">Filter orders created after this date (ISO 8601).</param>
    /// <param name="toDate">Filter orders created before this date (ISO 8601).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">List of order summaries.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderSummaryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? serviceLevel,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken ct)
    {
        var filter = new OrderFilter(
            status is not null ? Enum.Parse<Domain.Orders.Enums.OrderStatus>(status, ignoreCase: true) : null,
            serviceLevel is not null ? Enum.Parse<ServiceLevel>(serviceLevel, ignoreCase: true) : null,
            fromDate,
            toDate);

        var results = await _queries.DispatchAsync<IReadOnlyList<OrderSummary>>(
            new ListOrdersQuery(filter), ct);

        var response = results.Select(o => new OrderSummaryResponse(
            o.Id, o.OrderNumber, o.CustomerName,
            o.Status.ToString(), o.ServiceLevel.ToString(), o.CreatedAt
        )).ToList();

        return Ok(ApiResponse<IReadOnlyList<OrderSummaryResponse>>.Ok(response, _correlationId.GetCorrelationId()));
    }

    /// <summary>Confirm an order (Created → Confirmed).</summary>
    /// <remarks>
    /// Transitions the order from Created to Confirmed state via the State Pattern.
    /// Only valid from Created state — otherwise returns 400 with INVALID_TRANSITION.
    /// Dispatches ConfirmOrderCommand → ConfirmOrderHandler.
    /// </remarks>
    /// <param name="id">Order ID (GUID).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Order confirmed.</response>
    /// <response code="400">Invalid state transition.</response>
    /// <response code="404">Order not found.</response>
    [HttpPut("{id:guid}/confirm")]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(ApiResponse<OrderTransitionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm([FromRoute] Guid id, CancellationToken ct)
    {
        await _commands.DispatchAsync<bool>(new ConfirmOrderCommand(id), ct);

        return Ok(ApiResponse<OrderTransitionResponse>.Ok(
            new OrderTransitionResponse { OrderId = id, NewStatus = "Confirmed" },
            _correlationId.GetCorrelationId()));
    }

    /// <summary>Ship an order (Confirmed → Shipped).</summary>
    /// <remarks>
    /// Triggers the full shipping workflow:
    /// 1. Route calculation (Fastest strategy by default)
    /// 2. Carrier booking (cheapest quote selection)
    /// 3. State transition to Shipped with tracking number assignment
    ///
    /// Dispatches ShipOrderCommand → ShipOrderHandler.
    /// </remarks>
    /// <param name="id">Order ID (GUID).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Order shipped with carrier and tracking info.</response>
    /// <response code="400">Invalid state transition or routing/booking failure.</response>
    /// <response code="404">Order not found.</response>
    [HttpPut("{id:guid}/ship")]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(ApiResponse<OrderTransitionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ship([FromRoute] Guid id, CancellationToken ct)
    {
        var bookingResult = await _commands.DispatchAsync<BookingResult>(new ShipOrderCommand(id), ct);

        return Ok(ApiResponse<OrderTransitionResponse>.Ok(
            new OrderTransitionResponse
            {
                OrderId = id,
                NewStatus = "Shipped",
                CarrierCode = bookingResult.CarrierCode,
                TrackingNumber = bookingResult.TrackingNumber
            },
            _correlationId.GetCorrelationId()));
    }

    /// <summary>Mark an order as delivered (Shipped → Delivered).</summary>
    /// <remarks>
    /// Transitions the order from Shipped to Delivered (terminal state for successful delivery).
    /// Dispatches DeliverOrderCommand → DeliverOrderHandler.
    /// </remarks>
    /// <param name="id">Order ID (GUID).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Order delivered.</response>
    /// <response code="400">Invalid state transition.</response>
    /// <response code="404">Order not found.</response>
    [HttpPut("{id:guid}/deliver")]
    [Authorize(Roles = "Admin,Driver")]
    [ProducesResponseType(typeof(ApiResponse<OrderTransitionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deliver([FromRoute] Guid id, CancellationToken ct)
    {
        await _commands.DispatchAsync<bool>(new DeliverOrderCommand(id), ct);

        return Ok(ApiResponse<OrderTransitionResponse>.Ok(
            new OrderTransitionResponse { OrderId = id, NewStatus = "Delivered" },
            _correlationId.GetCorrelationId()));
    }

    /// <summary>Cancel an order (Created/Confirmed → Cancelled).</summary>
    /// <remarks>
    /// Cancels the order with an optional reason. Only valid from Created or Confirmed states.
    /// Once cancelled, no further transitions are allowed (terminal state).
    /// Dispatches CancelOrderCommand → CancelOrderHandler.
    /// </remarks>
    /// <param name="id">Order ID (GUID).</param>
    /// <param name="request">Optional cancellation reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Order cancelled.</response>
    /// <response code="400">Invalid state transition (e.g. cannot cancel shipped/delivered order).</response>
    /// <response code="404">Order not found.</response>
    [HttpPut("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(ApiResponse<OrderTransitionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid id,
        [FromBody] CancelOrderRequest? request,
        CancellationToken ct)
    {
        await _commands.DispatchAsync<bool>(new CancelOrderCommand(id, request?.Reason), ct);

        return Ok(ApiResponse<OrderTransitionResponse>.Ok(
            new OrderTransitionResponse { OrderId = id, NewStatus = "Cancelled" },
            _correlationId.GetCorrelationId()));
    }

    // ── Phase 9: Advanced Order Operations ───────────────────────

    /// <summary>Bulk-create multiple orders in a single request.</summary>
    /// <remarks>
    /// Creates 1-50 orders atomically. All orders are validated before any are created.
    /// If validation fails for any order, none are created — the response contains per-order errors.
    ///
    /// **Business rule**: Validate all → Process all → Return per-order results.
    /// Dispatches BulkCreateOrdersCommand → BulkCreateOrdersHandler.
    /// </remarks>
    /// <response code="200">Bulk creation results (may contain mix of success/failure).</response>
    /// <response code="400">Global validation error (e.g. more than 50 orders).</response>
    [HttpPost("bulk-create")]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(ApiResponse<BulkCreateOrdersResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateOrdersRequest request, CancellationToken ct)
    {
        var items = request.Orders.Select(o => new Application.Orders.Commands.BulkOrderItem(
            o.CustomerName,
            o.CustomerPhone,
            o.CustomerEmail,
            MapToAddress(o.Origin),
            MapToAddress(o.Destination),
            Enum.Parse<ServiceLevel>(o.ServiceLevel, ignoreCase: true),
            o.Items.Select(MapToOrderItem).ToList()
        )).ToList();

        var command = new Application.Orders.Commands.BulkCreateOrdersCommand(items);
        var result = await _commands.DispatchAsync<Application.Orders.Commands.BulkCreateResult>(command, ct);

        var response = new BulkCreateOrdersResponse
        {
            SuccessCount = result.SuccessCount,
            FailureCount = result.FailureCount,
            Results = result.Results.Select(r =>
                new BulkCreateItemResponse(r.Index, r.Success, r.OrderId, r.OrderNumber, r.Error)
            ).ToList()
        };

        return Ok(ApiResponse<BulkCreateOrdersResponse>.Ok(response, _correlationId.GetCorrelationId()));
    }

    /// <summary>Update an order's destination address.</summary>
    /// <remarks>
    /// Changes the delivery destination for an existing order.
    /// Only valid for orders in **Created** or **Confirmed** state.
    /// Shipped/Delivered/Cancelled orders cannot change destination.
    ///
    /// Dispatches UpdateDestinationCommand → UpdateDestinationHandler.
    /// </remarks>
    /// <param name="id">Order ID (GUID).</param>
    /// <param name="request">New destination address.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Destination updated successfully.</response>
    /// <response code="400">Invalid state or address.</response>
    /// <response code="404">Order not found.</response>
    [HttpPut("{id:guid}/update-destination")]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(ApiResponse<UpdateDestinationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDestination(
        [FromRoute] Guid id,
        [FromBody] UpdateDestinationRequest request,
        CancellationToken ct)
    {
        var newAddr = MapToAddress(request.Destination);
        var command = new Application.Orders.Commands.UpdateDestinationCommand(id, newAddr);
        await _commands.DispatchAsync<bool>(command, ct);

        // Fetch updated order for response
        var detail = await _queries.DispatchAsync<OrderDetail?>(new GetOrderByIdQuery(id), ct);

        return Ok(ApiResponse<UpdateDestinationResponse>.Ok(
            new UpdateDestinationResponse
            {
                OrderId = id,
                NewDestination = detail?.Destination ?? newAddr.ToShortString(),
                Status = detail?.Status.ToString() ?? "Unknown"
            },
            _correlationId.GetCorrelationId()));
    }

    /// <summary>Split an order into multiple shipments.</summary>
    /// <remarks>
    /// Splits an existing order based on item groupings. The original order is cancelled
    /// and new orders are created for each group, preserving customer info, addresses,
    /// and service level.
    ///
    /// **Rules**:
    /// - Only Created or Confirmed orders can be split
    /// - At least 2 groups required
    /// - All items must be assigned to exactly one group
    /// - Item indices are 0-based
    ///
    /// Dispatches SplitShipmentCommand → SplitShipmentHandler.
    /// </remarks>
    /// <param name="id">Order ID (GUID).</param>
    /// <param name="request">Split configuration with item groups.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Split completed with new order details.</response>
    /// <response code="400">Invalid split configuration or order state.</response>
    /// <response code="404">Order not found.</response>
    [HttpPost("{id:guid}/split-shipment")]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType(typeof(ApiResponse<SplitShipmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SplitShipment(
        [FromRoute] Guid id,
        [FromBody] SplitShipmentRequest request,
        CancellationToken ct)
    {
        var groups = request.Groups
            .Select(g => (IReadOnlyList<int>)g.AsReadOnly())
            .ToList();

        var command = new Application.Orders.Commands.SplitShipmentCommand(id, groups);
        var result = await _commands.DispatchAsync<Application.Orders.Commands.SplitShipmentResult>(command, ct);

        var response = new SplitShipmentResponse
        {
            OriginalOrderId = result.OriginalOrderId,
            NewOrders = result.NewOrders.Select(o =>
                new SplitOrderResponse(o.OrderId, o.OrderNumber, o.ItemCount)
            ).ToList()
        };

        return Ok(ApiResponse<SplitShipmentResponse>.Ok(response, _correlationId.GetCorrelationId()));
    }

    // ── Mapping helpers ──────────────────────────────────────────

    private static Address MapToAddress(OrderAddressDto dto)
    {
        return new Address(dto.Street, dto.City, dto.Province, dto.PostalCode, dto.Country);
    }

    private static OrderItem MapToOrderItem(OrderItemDto dto)
    {
        return new OrderItem(
            dto.Description,
            dto.Quantity,
            new Weight(dto.Weight.Value, Enum.Parse<WeightUnit>(dto.Weight.Unit, ignoreCase: true)),
            dto.Dimensions is not null
                ? new Dimension(dto.Dimensions.LengthCm, dto.Dimensions.WidthCm, dto.Dimensions.HeightCm)
                : null);
    }

    private static OrderDetailResponse MapToOrderDetailResponse(OrderDetail detail)
    {
        return new OrderDetailResponse
        {
            Id = detail.Id,
            OrderNumber = detail.OrderNumber,
            CustomerName = detail.CustomerName,
            Origin = detail.Origin,
            Destination = detail.Destination,
            Status = detail.Status.ToString(),
            ServiceLevel = detail.ServiceLevel.ToString(),
            TrackingNumber = detail.TrackingNumber,
            CarrierCode = detail.CarrierCode,
            Items = detail.Items.Select(i => new OrderItemResponse(
                i.Description,
                i.Quantity,
                new OrderWeightDto(i.Weight.Value, i.Weight.Unit.ToString()),
                i.Dimensions is not null
                    ? new OrderDimensionDto(i.Dimensions.LengthCm, i.Dimensions.WidthCm, i.Dimensions.HeightCm)
                    : null
            )).ToList(),
            CreatedAt = detail.CreatedAt,
            UpdatedAt = detail.UpdatedAt
        };
    }
}
