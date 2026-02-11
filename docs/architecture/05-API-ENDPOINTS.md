# 🌐 05-API-ENDPOINTS — Routes, Swagger, Request/Response

> **Framework**: ASP.NET Core 8, Controller-based API  
> **Swagger**: Swashbuckle.AspNetCore (sole external tooling package)  
> **Convention**: RESTful, kebab-case URLs, JSON request/response  
> **Base URL**: `https://localhost:5001/api`

---

## 📋 Table of Contents

1. [Endpoint Overview](#endpoint-overview)
2. [Routing Domain (01)](#routing-domain-01)
3. [Carrier Domain (02)](#carrier-domain-02)
4. [Tracking Domain (03)](#tracking-domain-03)
5. [Orders Domain (04)](#orders-domain-04)
6. [Audit Domain (05)](#audit-domain-05)
7. [Common Response Wrapper](#common-response-wrapper)
8. [Swagger Configuration](#swagger-configuration)
9. [Controller Code Patterns](#controller-code-patterns)
10. [Error Handling](#error-handling)

---

## Endpoint Overview

| # | Method | Path | Domain | Description |
|---|--------|------|--------|-------------|
| 1 | `POST` | `/api/routing/calculate` | Routing | Calculate route with specific strategy |
| 2 | `POST` | `/api/routing/compare` | Routing | Compare all strategies for same request |
| 3 | `GET` | `/api/routing/strategies` | Routing | List available strategy names |
| 4 | `GET` | `/api/carriers` | Carrier | List all registered carriers |
| 5 | `POST` | `/api/carriers/quotes` | Carrier | Get quotes from all carriers |
| 6 | `POST` | `/api/carriers/{code}/book` | Carrier | Book with specific carrier |
| 7 | `GET` | `/api/carriers/{code}/track/{trackingNo}` | Carrier | Track a shipment |
| 8 | `GET` | `/api/tracking/{trackingNo}/snapshot` | Tracking | Get current tracking snapshot |
| 9 | `POST` | `/api/tracking/{trackingNo}/subscribe` | Tracking | Subscribe to tracking updates (returns current snapshot) |
| 10 | `POST` | `/api/orders` | Orders | Create a new order |
| 11 | `GET` | `/api/orders/{id}` | Orders | Get order detail |
| 12 | `GET` | `/api/orders` | Orders | List orders with filters |
| 13 | `PUT` | `/api/orders/{id}/confirm` | Orders | Confirm an order |
| 14 | `PUT` | `/api/orders/{id}/ship` | Orders | Ship an order (triggers routing + booking) |
| 15 | `PUT` | `/api/orders/{id}/deliver` | Orders | Mark order as delivered |
| 16 | `PUT` | `/api/orders/{id}/cancel` | Orders | Cancel an order |
| 17 | `GET` | `/api/audit/entity/{entityType}/{entityId}` | Audit | Get audit trail for an entity |
| 18 | `GET` | `/api/audit/correlation/{correlationId}` | Audit | Get audit trail by correlation ID |

**Total endpoints: 18**

---

## Routing Domain (01)

### `POST /api/routing/calculate`

Calculate a route using a specific strategy.

**Request Body:**
```json
{
  "origin": { "latitude": 31.2304, "longitude": 121.4737 },
  "destination": { "latitude": 39.9042, "longitude": 116.4074 },
  "packageWeight": { "value": 5.5, "unit": "Kg" },
  "serviceLevel": "Express",
  "strategy": "Fastest"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "strategyUsed": "Fastest",
    "waypointNodeIds": ["SH-01", "NJ-01", "JN-01", "BJ-01"],
    "distanceKm": 1214.5,
    "estimatedDuration": "12:30:00",
    "estimatedCost": { "amount": 35.00, "currency": "CNY" }
  },
  "correlationId": "abc-123"
}
```

**Error (400 Bad Request):**
```json
{
  "success": false,
  "error": { "code": "STRATEGY_NOT_FOUND", "message": "No strategy registered with name: Unknown" },
  "correlationId": "abc-123"
}
```

### `POST /api/routing/compare`

Compare all strategies for the same request.

**Request Body:** Same as calculate (without `strategy` field).

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "strategyUsed": "Fastest",
      "distanceKm": 1214.5,
      "estimatedDuration": "12:30:00",
      "estimatedCost": { "amount": 55.00, "currency": "CNY" }
    },
    {
      "strategyUsed": "Cheapest",
      "distanceKm": 1350.2,
      "estimatedDuration": "18:45:00",
      "estimatedCost": { "amount": 28.00, "currency": "CNY" }
    },
    {
      "strategyUsed": "Balanced",
      "distanceKm": 1280.0,
      "estimatedDuration": "15:00:00",
      "estimatedCost": { "amount": 38.00, "currency": "CNY" }
    }
  ],
  "correlationId": "abc-124"
}
```

### `GET /api/routing/strategies`

**Response (200 OK):**
```json
{
  "success": true,
  "data": ["Fastest", "Cheapest", "Balanced"],
  "correlationId": "abc-125"
}
```

---

## Carrier Domain (02)

### `GET /api/carriers`

List all registered carriers.

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    { "carrierCode": "SF", "name": "SF Express (顺丰速运)" },
    { "carrierCode": "JD", "name": "JD Logistics (京东物流)" }
  ]
}
```

### `POST /api/carriers/quotes`

Get quotes from all carriers.

**Request Body:**
```json
{
  "origin": { "street": "浦东新区张江", "city": "上海", "province": "上海", "postalCode": "201203", "country": "CN" },
  "destination": { "street": "海淀区中关村", "city": "北京", "province": "北京", "postalCode": "100080", "country": "CN" },
  "weight": { "value": 3.0, "unit": "Kg" },
  "serviceLevel": "Standard"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "quotes": [
      { "carrierCode": "SF", "price": { "amount": 23.00, "currency": "CNY" }, "estimatedDays": 2, "serviceLevel": "Standard" },
      { "carrierCode": "JD", "price": { "amount": 18.50, "currency": "CNY" }, "estimatedDays": 3, "serviceLevel": "Standard" }
    ],
    "recommended": { "carrierCode": "JD", "reason": "Cheapest" }
  }
}
```

### `POST /api/carriers/{code}/book`

Book a shipment with a specific carrier.

**Request Body:**
```json
{
  "origin": { "street": "浦东新区张江", "city": "上海", "province": "上海", "postalCode": "201203", "country": "CN" },
  "destination": { "street": "海淀区中关村", "city": "北京", "province": "北京", "postalCode": "100080", "country": "CN" },
  "weight": { "value": 3.0, "unit": "Kg" },
  "sender": { "name": "张三", "phone": "13812345678", "email": "zhang@example.com" },
  "recipient": { "name": "李四", "phone": "13987654321" },
  "serviceLevel": "Standard"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "carrierCode": "SF",
    "trackingNumber": "SF1234567890",
    "bookedAt": "2026-02-08T10:30:00Z"
  }
}
```

### `GET /api/carriers/{code}/track/{trackingNo}`

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "trackingNumber": "SF1234567890",
    "status": "InTransit",
    "currentLocation": "南京转运中心",
    "updatedAt": "2026-02-08T14:00:00Z"
  }
}
```

---

## Tracking Domain (03)

### `GET /api/tracking/{trackingNo}/snapshot`

Get the latest known snapshot for a tracking number.

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "trackingNumber": "SF1234567890",
    "currentStatus": "InTransit",
    "lastLocation": { "latitude": 32.0603, "longitude": 118.7969 },
    "updatedAt": "2026-02-08T14:00:00Z"
  }
}
```

**Response (404 Not Found):**
```json
{
  "success": false,
  "error": { "code": "NOT_FOUND", "message": "No tracking data for SF1234567890" }
}
```

### `POST /api/tracking/{trackingNo}/subscribe`

Subscribe to tracking updates (returns current snapshot as acknowledgment).

> Note: In a real system, this would upgrade to WebSocket/SSE. For our demo,  
> this registers a console observer and returns the current snapshot.

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "subscribed": true,
    "trackingNumber": "SF1234567890",
    "currentSnapshot": {
      "currentStatus": "InTransit",
      "lastLocation": { "latitude": 32.0603, "longitude": 118.7969 },
      "updatedAt": "2026-02-08T14:00:00Z"
    }
  }
}
```

---

## Orders Domain (04)

### `POST /api/orders`

Create a new order (CQRS Command: `CreateOrderCommand`).

**Request Body:**
```json
{
  "customerName": "张三",
  "customerPhone": "13812345678",
  "customerEmail": "zhang@example.com",
  "origin": { "street": "浦东新区张江", "city": "上海", "province": "上海", "postalCode": "201203", "country": "CN" },
  "destination": { "street": "海淀区中关村", "city": "北京", "province": "北京", "postalCode": "100080", "country": "CN" },
  "serviceLevel": "Express",
  "items": [
    { "description": "电子产品", "quantity": 1, "weight": { "value": 2.5, "unit": "Kg" }, "dimensions": { "lengthCm": 30, "widthCm": 20, "heightCm": 15 } }
  ]
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "data": {
    "orderId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "orderNumber": "DT-20260208-001",
    "status": "Created"
  },
  "correlationId": "req-001"
}
```

### `GET /api/orders/{id}`

Get order detail (CQRS Query: `GetOrderByIdQuery`).

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "orderNumber": "DT-20260208-001",
    "customerName": "张三",
    "origin": "上海, 上海 201203",
    "destination": "北京, 北京 100080",
    "status": "Confirmed",
    "serviceLevel": "Express",
    "trackingNumber": null,
    "carrierCode": null,
    "items": [
      { "description": "电子产品", "quantity": 1, "weight": { "value": 2.5, "unit": "Kg" } }
    ],
    "createdAt": "2026-02-08T10:00:00Z",
    "updatedAt": "2026-02-08T10:05:00Z"
  }
}
```

### `GET /api/orders`

List orders with optional filters (CQRS Query: `ListOrdersQuery`).

**Query Parameters:**
- `status` (optional): `Created`, `Confirmed`, `Shipped`, `Delivered`, `Cancelled`
- `serviceLevel` (optional): `Express`, `Standard`, `Economy`
- `fromDate` (optional): ISO 8601
- `toDate` (optional): ISO 8601

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": "a1b2c3d4-...",
      "orderNumber": "DT-20260208-001",
      "customerName": "张三",
      "status": "Confirmed",
      "serviceLevel": "Express",
      "createdAt": "2026-02-08T10:00:00Z"
    }
  ]
}
```

### `PUT /api/orders/{id}/confirm`

Transition order: Created → Confirmed (CQRS Command: `ConfirmOrderCommand`).

**Request Body:** None (empty or `{}`).

**Response (200 OK):**
```json
{ "success": true, "data": { "orderId": "a1b2c3d4-...", "newStatus": "Confirmed" } }
```

**Response (400 — invalid transition):**
```json
{ "success": false, "error": { "code": "INVALID_TRANSITION", "message": "Cannot Confirm from Delivered state" } }
```

### `PUT /api/orders/{id}/ship`

Transition order: Confirmed → Shipped. This triggers routing + carrier booking.

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "orderId": "a1b2c3d4-...",
    "newStatus": "Shipped",
    "carrierCode": "SF",
    "trackingNumber": "SF1234567890"
  }
}
```

### `PUT /api/orders/{id}/deliver`

Transition order: Shipped → Delivered.

**Response (200 OK):**
```json
{ "success": true, "data": { "orderId": "a1b2c3d4-...", "newStatus": "Delivered" } }
```

### `PUT /api/orders/{id}/cancel`

Transition order: Created/Confirmed → Cancelled.

**Request Body:**
```json
{ "reason": "Customer requested cancellation" }
```

**Response (200 OK):**
```json
{ "success": true, "data": { "orderId": "a1b2c3d4-...", "newStatus": "Cancelled" } }
```

---

## Audit Domain (05)

### `GET /api/audit/entity/{entityType}/{entityId}`

Get audit trail for a specific entity.

**Example:** `GET /api/audit/entity/Order/a1b2c3d4-e5f6-7890-abcd-ef1234567890`

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": "aud-001",
      "entityType": "Order",
      "entityId": "a1b2c3d4-...",
      "action": "Created",
      "category": "DataChange",
      "actor": "system",
      "correlationId": "req-001",
      "timestamp": "2026-02-08T10:00:00Z",
      "description": "Order created: DT-20260208-001"
    },
    {
      "id": "aud-002",
      "entityType": "Order",
      "entityId": "a1b2c3d4-...",
      "action": "StateChanged",
      "category": "StateTransition",
      "actor": "system",
      "correlationId": "req-002",
      "timestamp": "2026-02-08T10:05:00Z",
      "description": "State: Created → Confirmed"
    }
  ]
}
```

### `GET /api/audit/correlation/{correlationId}`

Get all audit records for a single request/correlation.

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    { "id": "aud-003", "entityType": "Order", "action": "StateChanged", "description": "State: Confirmed → Shipped", "timestamp": "2026-02-08T11:00:00Z" },
    { "id": "aud-004", "entityType": "Route", "action": "BusinessAction", "description": "Route calculated: Fastest, 1214.5km", "timestamp": "2026-02-08T11:00:01Z" },
    { "id": "aud-005", "entityType": "Carrier", "action": "BusinessAction", "description": "Booked SF Express: SF1234567890", "timestamp": "2026-02-08T11:00:02Z" }
  ]
}
```

---

## Common Response Wrapper

### `ApiResponse<T>`

```csharp
// src/DtExpress.Api/Models/ApiResponse.cs

namespace DtExpress.Api.Models;

/// <summary>Unified API response envelope.</summary>
public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public string? CorrelationId { get; init; }

    public static ApiResponse<T> Ok(T data, string? correlationId = null)
        => new() { Success = true, Data = data, CorrelationId = correlationId };

    public static ApiResponse<T> Fail(string code, string message, string? correlationId = null)
        => new() { Success = false, Error = new ApiError(code, message), CorrelationId = correlationId };
}

public sealed record ApiError(string Code, string Message);
```

---

## Swagger Configuration

### Tags (grouped by domain)

| Tag | Controllers | Description |
|-----|-------------|-------------|
| `Routing` | `RoutingController` | Route calculation and strategy comparison |
| `Carriers` | `CarrierController` | Carrier quotes, booking, tracking |
| `Tracking` | `TrackingController` | Real-time tracking snapshots and subscriptions |
| `Orders` | `OrdersController` | Order lifecycle (CQRS commands + queries) |
| `Audit` | `AuditController` | Audit trail queries |

### Swagger Annotations Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    /// <summary>Create a new order.</summary>
    /// <response code="201">Order created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateOrderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request) { /* ... */ }
}
```

### Swagger URL

- **UI**: `https://localhost:5001/swagger`
- **JSON**: `https://localhost:5001/swagger/v1/swagger.json`

---

## Controller Code Patterns

### Standard Controller Structure

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RoutingController : ControllerBase
{
    private readonly RouteCalculationService _calculationService;
    private readonly RouteComparisonService _comparisonService;
    private readonly IRouteStrategyFactory _strategyFactory;
    private readonly ICorrelationIdProvider _correlationId;

    public RoutingController(
        RouteCalculationService calculationService,
        RouteComparisonService comparisonService,
        IRouteStrategyFactory strategyFactory,
        ICorrelationIdProvider correlationId)
    {
        _calculationService = calculationService;
        _comparisonService = comparisonService;
        _strategyFactory = strategyFactory;
        _correlationId = correlationId;
    }

    [HttpPost("calculate")]
    public IActionResult Calculate([FromBody] CalculateRouteRequest request)
    {
        // Delegate to application service — controller does NOT contain business logic
    }
}
```

### Orders Controller (CQRS Pattern)

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;
    private readonly ICorrelationIdProvider _correlationId;

    // Commands go through dispatcher → handler
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderApiRequest request)
    {
        var command = new CreateOrderCommand(/* map from request */);
        var orderId = await _commands.DispatchAsync<Guid>(command);
        return CreatedAtAction(nameof(GetById), new { id = orderId },
            ApiResponse<object>.Ok(new { orderId, status = "Created" }, _correlationId.GetCorrelationId()));
    }

    // Queries go through dispatcher → handler
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetOrderByIdQuery(id);
        var result = await _queries.DispatchAsync<OrderDetail?>(query);
        if (result is null) return NotFound(ApiResponse<object>.Fail("NOT_FOUND", $"Order {id} not found"));
        return Ok(ApiResponse<OrderDetail>.Ok(result, _correlationId.GetCorrelationId()));
    }
}
```

---

## Error Handling

### Domain Exception → HTTP Status Mapping

| Exception | HTTP Status | Error Code |
|-----------|-------------|------------|
| `InvalidStateTransitionException` | 400 Bad Request | `INVALID_TRANSITION` |
| `CarrierNotFoundException` | 404 Not Found | `CARRIER_NOT_FOUND` |
| `StrategyNotFoundException` | 400 Bad Request | `STRATEGY_NOT_FOUND` |
| `DomainException` (generic) | 400 Bad Request | Dynamic `Code` |
| `ArgumentException` | 400 Bad Request | `VALIDATION_ERROR` |
| Unhandled | 500 Internal Server Error | `INTERNAL_ERROR` |

### Exception Handling in Controllers

```csharp
// Pattern: try/catch in each action, or use a global exception filter
// For simplicity, use a try/catch pattern in each controller action:

try
{
    var result = await _commands.DispatchAsync<Guid>(command);
    return Ok(ApiResponse<object>.Ok(result));
}
catch (DomainException ex)
{
    return BadRequest(ApiResponse<object>.Fail(ex.Code, ex.Message, _correlationId.GetCorrelationId()));
}
```

> **Alternative**: A global `ExceptionFilterAttribute` could centralize this.
> For interview clarity, explicit try/catch is more readable.
