# DT-Express TMS — API Reference Guide

> **Base URL**: `http://localhost:5198`  
> **Authentication**: JWT Bearer token (`Authorization: Bearer {token}`)  
> **Content-Type**: `application/json` (all request/response bodies)  
> **Character Encoding**: UTF-8 (full Chinese character support)

---

## Table of Contents

1. [Response Envelope](#response-envelope)
2. [Error Codes](#error-codes)
3. [Authentication (3 endpoints)](#authentication)
4. [Orders (7 endpoints)](#orders)
5. [Routing (3 endpoints)](#routing)
6. [Carriers (4 endpoints)](#carriers)
7. [Tracking (2 endpoints)](#tracking)
8. [Audit (2 endpoints)](#audit)
9. [Dashboard (3 endpoints)](#dashboard)
10. [Advanced Orders (3 endpoints)](#advanced-orders)
11. [Webhooks (1 endpoint)](#webhooks)
12. [Reports (2 endpoints)](#reports)
13. [Real-Time — SignalR Hub](#real-time--signalr-hub)

---

## Response Envelope

Every response uses the `ApiResponse<T>` envelope:

```json
{
  "success": true,
  "data": { ... },
  "error": null,
  "correlationId": "abc-123"
}
```

**Error response:**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message"
  },
  "correlationId": "abc-123"
}
```

Pass `X-Correlation-ID` header to set a custom correlation ID for request tracing (auto-generated if omitted).

---

## Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `AUTH_FAILED` | 400 | Invalid username or password |
| `USERNAME_TAKEN` | 400 | Username already exists |
| `EMAIL_TAKEN` | 400 | Email already registered |
| `INVALID_ROLE` | 400 | Role must be Admin, Dispatcher, Driver, or Viewer |
| `INVALID_REFRESH_TOKEN` | 400 | Refresh token is invalid or expired |
| `NOT_FOUND` | 404 | Resource not found |
| `INVALID_TRANSITION` | 400 | Invalid state transition (e.g. confirm a delivered order) |
| `CARRIER_NOT_FOUND` | 404 | Carrier code not recognized |
| `STRATEGY_NOT_FOUND` | 400 | Routing strategy not recognized |
| `VALIDATION_ERROR` | 400 | Request body failed validation |

---

## Authentication

### 1. POST /api/auth/login

Login with username and password to receive JWT tokens.

| Property | Value |
|----------|-------|
| **Auth Required** | ❌ No |
| **Roles** | Any (anonymous) |

**Request Body:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "expiresAt": "2026-02-11T14:00:00+00:00",
    "userId": "a0000000-0000-0000-0000-000000000001",
    "username": "admin",
    "displayName": "系统管理员",
    "role": "Admin"
  },
  "error": null,
  "correlationId": "auto-generated-uuid"
}
```

**Error Response (400):**
```json
{
  "success": false,
  "data": null,
  "error": { "code": "AUTH_FAILED", "message": "Invalid username or password." },
  "correlationId": "..."
}
```

**Test Accounts:**

| Username | Password | Role | Display Name |
|----------|----------|------|-------------|
| `admin` | `admin123` | Admin | 系统管理员 |
| `dispatcher` | `passwd123` | Dispatcher | 调度员小李 |
| `driver` | `passwd123` | Driver | 司机王师傅 |
| `viewer` | `passwd123` | Viewer | 客服张小姐 |

**curl:**
```bash
curl -X POST http://localhost:5198/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin123"}'
```

---

### 2. POST /api/auth/register

Register a new user account and receive JWT tokens immediately.

| Property | Value |
|----------|-------|
| **Auth Required** | ❌ No |
| **Roles** | Any (anonymous) |

**Request Body:**
```json
{
  "username": "newuser",
  "email": "newuser@dtexpress.com",
  "password": "securepass123",
  "displayName": "新用户",
  "role": "Viewer"
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "...",
    "expiresAt": "2026-02-11T14:00:00+00:00",
    "userId": "generated-uuid",
    "username": "newuser",
    "displayName": "新用户",
    "role": "Viewer"
  },
  "error": null,
  "correlationId": "..."
}
```

**Business Rules:**
- Username must be unique (case-insensitive)
- Email must be unique
- Valid roles: `Admin`, `Dispatcher`, `Driver`, `Viewer`
- Password is hashed with BCrypt (work factor 12)

**curl:**
```bash
curl -X POST http://localhost:5198/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "newuser",
    "email": "newuser@dtexpress.com",
    "password": "securepass123",
    "displayName": "新用户",
    "role": "Viewer"
  }'
```

---

### 3. POST /api/auth/refresh

Exchange a refresh token for a new access token + refresh token pair.

| Property | Value |
|----------|-------|
| **Auth Required** | ❌ No |
| **Roles** | Any (anonymous) |

**Request Body:**
```json
{
  "refreshToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

**Response (200 OK):** Same as login response.

**Business Rules:**
- Refresh tokens are **single-use** (consumed after exchange)
- Using a consumed refresh token returns `INVALID_REFRESH_TOKEN`

**curl:**
```bash
curl -X POST http://localhost:5198/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"}'
```

---

## Orders

### 4. POST /api/orders

Create a new order with customer info, addresses, service level, and items.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | `Admin`, `Dispatcher` |

**Request Body:**
```json
{
  "customerName": "张三",
  "customerPhone": "13812345678",
  "customerEmail": "zhangsan@example.com",
  "origin": {
    "street": "浦东新区陆家嘴环路1000号",
    "city": "上海",
    "province": "Shanghai",
    "postalCode": "200120",
    "country": "CN"
  },
  "destination": {
    "street": "天河区珠江新城花城大道18号",
    "city": "广州",
    "province": "Guangdong",
    "postalCode": "510623",
    "country": "CN"
  },
  "serviceLevel": "Express",
  "items": [
    {
      "description": "电子产品 - 笔记本电脑",
      "quantity": 1,
      "weight": { "value": 2.5, "unit": "Kg" },
      "dimensions": { "lengthCm": 35.0, "widthCm": 25.0, "heightCm": 3.0 }
    },
    {
      "description": "配件 - 充电器和鼠标",
      "quantity": 2,
      "weight": { "value": 0.3, "unit": "Kg" },
      "dimensions": null
    }
  ]
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "data": {
    "orderId": "generated-uuid",
    "orderNumber": "DT-20260211-001",
    "status": "Created"
  },
  "error": null,
  "correlationId": "..."
}
```

**Field Reference:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `customerName` | string | ✅ | Customer full name (supports Chinese: 张三) |
| `customerPhone` | string | ✅ | Chinese mobile: `1[3-9]XXXXXXXXX` |
| `customerEmail` | string | ❌ | Optional email address |
| `origin` | Address | ✅ | Pickup address |
| `destination` | Address | ✅ | Delivery address |
| `serviceLevel` | string | ✅ | `Express`, `Standard`, or `Economy` |
| `items` | array | ✅ | At least 1 item |
| `items[].description` | string | ✅ | Item description (Chinese OK) |
| `items[].quantity` | int | ✅ | Must be ≥ 1 |
| `items[].weight.value` | decimal | ✅ | Must be > 0 |
| `items[].weight.unit` | string | ✅ | `Kg`, `G`, `Jin`, or `Lb` |
| `items[].dimensions` | object | ❌ | All-or-nothing (length, width, height) |

**Address Schema:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `street` | string | ✅ | Street address (Chinese OK: 浦东新区陆家嘴环路1000号) |
| `city` | string | ✅ | City (上海, 北京, 广州) |
| `province` | string | ✅ | Province name |
| `postalCode` | string | ✅ | 6-digit Chinese postal code |
| `country` | string | ❌ | ISO country code (default: `CN`) |

**Weight Units:**

| Unit | Name | Example |
|------|------|---------|
| `Kg` | Kilogram | 2.5 Kg |
| `G` | Gram | 300 G |
| `Jin` | Chinese 斤 (0.5 Kg) | 5 Jin = 2.5 Kg |
| `Lb` | Pound | 5.5 Lb |

**curl:**
```bash
TOKEN="eyJhbGciOiJIUzI1NiIs..."

curl -X POST http://localhost:5198/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Correlation-ID: order-create-001" \
  -d '{
    "customerName": "张三",
    "customerPhone": "13812345678",
    "customerEmail": "zhangsan@example.com",
    "origin": {
      "street": "浦东新区陆家嘴环路1000号",
      "city": "上海",
      "province": "Shanghai",
      "postalCode": "200120",
      "country": "CN"
    },
    "destination": {
      "street": "天河区珠江新城花城大道18号",
      "city": "广州",
      "province": "Guangdong",
      "postalCode": "510623",
      "country": "CN"
    },
    "serviceLevel": "Express",
    "items": [
      {
        "description": "电子产品 - 笔记本电脑",
        "quantity": 1,
        "weight": { "value": 2.5, "unit": "Kg" },
        "dimensions": { "lengthCm": 35.0, "widthCm": 25.0, "heightCm": 3.0 }
      }
    ]
  }'
```

---

### 5. GET /api/orders

List orders with optional filters.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Any authenticated user |

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `status` | string | ❌ | Filter: `Created`, `Confirmed`, `Shipped`, `Delivered`, `Cancelled` |
| `serviceLevel` | string | ❌ | Filter: `Express`, `Standard`, `Economy` |
| `fromDate` | DateTimeOffset | ❌ | Orders created after this date (ISO 8601) |
| `toDate` | DateTimeOffset | ❌ | Orders created before this date (ISO 8601) |

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": "b0000000-0000-0000-0000-000000000001",
      "orderNumber": "DT-20260211-001",
      "customerName": "张三",
      "status": "Created",
      "serviceLevel": "Express",
      "createdAt": "2026-02-11T10:00:00+00:00"
    }
  ],
  "error": null,
  "correlationId": "..."
}
```

**curl:**
```bash
# List all orders
curl http://localhost:5198/api/orders \
  -H "Authorization: Bearer $TOKEN"

# Filter by status
curl "http://localhost:5198/api/orders?status=Created&serviceLevel=Express" \
  -H "Authorization: Bearer $TOKEN"
```

---

### 6. GET /api/orders/{id}

Get full order detail by ID.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Any authenticated user |

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | GUID | Order ID |

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "b0000000-0000-0000-0000-000000000001",
    "orderNumber": "DT-20260211-001",
    "customerName": "张三",
    "origin": "浦东新区陆家嘴环路1000号, 上海, Shanghai 200120, CN",
    "destination": "天河区珠江新城花城大道18号, 广州, Guangdong 510623, CN",
    "status": "Created",
    "serviceLevel": "Express",
    "trackingNumber": null,
    "carrierCode": null,
    "items": [
      {
        "description": "电子产品 - 笔记本电脑",
        "quantity": 1,
        "weight": { "value": 2.5, "unit": "Kg" },
        "dimensions": { "lengthCm": 35.0, "widthCm": 25.0, "heightCm": 3.0 }
      }
    ],
    "createdAt": "2026-02-11T10:00:00+00:00",
    "updatedAt": "2026-02-11T10:00:00+00:00"
  },
  "error": null,
  "correlationId": "..."
}
```

**Error (404):**
```json
{
  "success": false,
  "data": null,
  "error": { "code": "NOT_FOUND", "message": "Order {id} not found." },
  "correlationId": "..."
}
```

**curl:**
```bash
curl http://localhost:5198/api/orders/b0000000-0000-0000-0000-000000000001 \
  -H "Authorization: Bearer $TOKEN"
```

---

### 7. PUT /api/orders/{id}/confirm

Transition order from `Created` → `Confirmed`.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | `Admin`, `Dispatcher` |

**Path Parameters:** `id` (GUID) — Order ID

**Request Body:** None

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "orderId": "b0000000-...",
    "newStatus": "Confirmed",
    "carrierCode": null,
    "trackingNumber": null
  },
  "error": null,
  "correlationId": "..."
}
```

**Business Rules:**
- Only valid from `Created` state
- Invalid transition → 400 `INVALID_TRANSITION`

**curl:**
```bash
curl -X PUT http://localhost:5198/api/orders/{id}/confirm \
  -H "Authorization: Bearer $TOKEN"
```

---

### 8. PUT /api/orders/{id}/ship

Transition order from `Confirmed` → `Shipped`. Triggers automatic routing + carrier booking.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | `Admin`, `Dispatcher` |

**Path Parameters:** `id` (GUID) — Order ID

**Request Body:** None

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "orderId": "b0000000-...",
    "newStatus": "Shipped",
    "carrierCode": "SF",
    "trackingNumber": "SF0000000001"
  },
  "error": null,
  "correlationId": "..."
}
```

**Business Rules:**
- Only valid from `Confirmed` state
- Automatically calculates route (Fastest strategy)
- Automatically selects cheapest carrier quote and books
- Returns assigned carrier code and tracking number

**curl:**
```bash
curl -X PUT http://localhost:5198/api/orders/{id}/ship \
  -H "Authorization: Bearer $TOKEN"
```

---

### 9. PUT /api/orders/{id}/deliver

Transition order from `Shipped` → `Delivered` (terminal state).

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | `Admin`, `Driver` |

**Path Parameters:** `id` (GUID) — Order ID

**Request Body:** None

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "orderId": "b0000000-...",
    "newStatus": "Delivered",
    "carrierCode": null,
    "trackingNumber": null
  },
  "error": null,
  "correlationId": "..."
}
```

**Business Rules:**
- Only valid from `Shipped` state
- Terminal state — no further transitions allowed
- Only Admin or Driver roles can deliver

**curl:**
```bash
curl -X PUT http://localhost:5198/api/orders/{id}/deliver \
  -H "Authorization: Bearer $TOKEN"
```

---

### 10. PUT /api/orders/{id}/cancel

Cancel an order with optional reason.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | `Admin`, `Dispatcher` |

**Path Parameters:** `id` (GUID) — Order ID

**Request Body (optional):**
```json
{
  "reason": "客户要求取消"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "orderId": "b0000000-...",
    "newStatus": "Cancelled",
    "carrierCode": null,
    "trackingNumber": null
  },
  "error": null,
  "correlationId": "..."
}
```

**Business Rules:**
- Valid from `Created` or `Confirmed` states only
- Cannot cancel `Shipped`, `Delivered`, or already `Cancelled` orders → 400 `INVALID_TRANSITION`
- Terminal state — no further transitions allowed

**curl:**
```bash
curl -X PUT http://localhost:5198/api/orders/{id}/cancel \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason": "客户要求取消"}'
```

---

## Routing

### 11. POST /api/routing/calculate

Calculate a route using a specific strategy.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Any authenticated user |

**Request Body:**
```json
{
  "origin": { "latitude": 31.2304, "longitude": 121.4737 },
  "destination": { "latitude": 39.9042, "longitude": 116.4074 },
  "packageWeight": { "value": 2.5, "unit": "Kg" },
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
    "waypointNodeIds": ["NODE-SH", "NODE-NJ", "NODE-BJ"],
    "distanceKm": 1200.5,
    "estimatedDuration": "14:30:00",
    "estimatedCost": { "amount": 45.00, "currency": "CNY" }
  },
  "error": null,
  "correlationId": "..."
}
```

**Strategies:**

| Strategy | Algorithm | Optimization |
|----------|-----------|-------------|
| `Fastest` | A* pathfinding | Minimize travel time |
| `Cheapest` | Dijkstra by cost | Minimize shipping cost |
| `Balanced` | Weighted composite | 60% time + 40% cost |

**City Coordinates Reference:**

| City | Latitude | Longitude |
|------|----------|-----------|
| 上海 Shanghai | 31.2304 | 121.4737 |
| 北京 Beijing | 39.9042 | 116.4074 |
| 广州 Guangzhou | 23.1291 | 113.2644 |
| 深圳 Shenzhen | 22.5431 | 114.0579 |
| 成都 Chengdu | 30.5728 | 104.0668 |

**curl:**
```bash
curl -X POST http://localhost:5198/api/routing/calculate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "origin": { "latitude": 31.2304, "longitude": 121.4737 },
    "destination": { "latitude": 39.9042, "longitude": 116.4074 },
    "packageWeight": { "value": 2.5, "unit": "Kg" },
    "serviceLevel": "Express",
    "strategy": "Fastest"
  }'
```

---

### 12. POST /api/routing/compare

Compare all registered strategies for the same request.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Any authenticated user |

**Request Body:**
```json
{
  "origin": { "latitude": 31.2304, "longitude": 121.4737 },
  "destination": { "latitude": 23.1291, "longitude": 113.2644 },
  "packageWeight": { "value": 5.0, "unit": "Jin" },
  "serviceLevel": "Standard"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "strategyUsed": "Fastest",
      "waypointNodeIds": ["NODE-SH", "NODE-GZ"],
      "distanceKm": 1500.0,
      "estimatedDuration": "18:00:00",
      "estimatedCost": { "amount": 55.00, "currency": "CNY" }
    },
    {
      "strategyUsed": "Cheapest",
      "waypointNodeIds": ["NODE-SH", "NODE-WH", "NODE-GZ"],
      "distanceKm": 1800.0,
      "estimatedDuration": "24:00:00",
      "estimatedCost": { "amount": 35.00, "currency": "CNY" }
    },
    {
      "strategyUsed": "Balanced",
      "waypointNodeIds": ["NODE-SH", "NODE-GZ"],
      "distanceKm": 1500.0,
      "estimatedDuration": "20:00:00",
      "estimatedCost": { "amount": 42.00, "currency": "CNY" }
    }
  ],
  "error": null,
  "correlationId": "..."
}
```

**curl:**
```bash
curl -X POST http://localhost:5198/api/routing/compare \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "origin": { "latitude": 31.2304, "longitude": 121.4737 },
    "destination": { "latitude": 23.1291, "longitude": 113.2644 },
    "packageWeight": { "value": 5.0, "unit": "Jin" },
    "serviceLevel": "Standard"
  }'
```

---

### 13. GET /api/routing/strategies

List all available routing strategy names.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Any authenticated user |

**Response (200 OK):**
```json
{
  "success": true,
  "data": ["Fastest", "Cheapest", "Balanced"],
  "error": null,
  "correlationId": "..."
}
```

**curl:**
```bash
curl http://localhost:5198/api/routing/strategies \
  -H "Authorization: Bearer $TOKEN"
```

---

## Carriers

### 14. GET /api/carriers

List all registered carrier adapters.

| Property | Value |
|----------|-------|
| **Auth Required** | ❌ No (public) |
| **Roles** | Anonymous |

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    { "carrierCode": "SF", "name": "SF Express (顺丰速运)" },
    { "carrierCode": "JD", "name": "JD Logistics (京东物流)" }
  ],
  "error": null,
  "correlationId": "..."
}
```

**curl:**
```bash
curl http://localhost:5198/api/carriers
```

---

### 15. POST /api/carriers/quotes

Get shipping quotes from all registered carriers.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Any authenticated user |

**Request Body:**
```json
{
  "origin": {
    "street": "浦东新区陆家嘴环路1000号",
    "city": "上海",
    "province": "Shanghai",
    "postalCode": "200120",
    "country": "CN"
  },
  "destination": {
    "street": "朝阳区建国门外大街1号",
    "city": "北京",
    "province": "Beijing",
    "postalCode": "100020",
    "country": "CN"
  },
  "weight": { "value": 2.5, "unit": "Kg" },
  "serviceLevel": "Express"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "quotes": [
      {
        "carrierCode": "SF",
        "price": { "amount": 35.50, "currency": "CNY" },
        "estimatedDays": 1,
        "serviceLevel": "Express"
      },
      {
        "carrierCode": "JD",
        "price": { "amount": 28.00, "currency": "CNY" },
        "estimatedDays": 2,
        "serviceLevel": "Express"
      }
    ],
    "recommended": {
      "carrierCode": "JD",
      "reason": "Cheapest"
    }
  },
  "error": null,
  "correlationId": "..."
}
```

**curl:**
```bash
curl -X POST http://localhost:5198/api/carriers/quotes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "origin": {
      "street": "浦东新区陆家嘴环路1000号",
      "city": "上海",
      "province": "Shanghai",
      "postalCode": "200120",
      "country": "CN"
    },
    "destination": {
      "street": "朝阳区建国门外大街1号",
      "city": "北京",
      "province": "Beijing",
      "postalCode": "100020",
      "country": "CN"
    },
    "weight": { "value": 2.5, "unit": "Kg" },
    "serviceLevel": "Express"
  }'
```

---

### 16. POST /api/carriers/{code}/book

Book a shipment with a specific carrier.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | `Admin`, `Dispatcher` |

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `code` | string | Carrier code: `SF` or `JD` (case-insensitive) |

**Request Body:**
```json
{
  "origin": {
    "street": "浦东新区陆家嘴环路1000号",
    "city": "上海",
    "province": "Shanghai",
    "postalCode": "200120",
    "country": "CN"
  },
  "destination": {
    "street": "天河区珠江新城花城大道18号",
    "city": "广州",
    "province": "Guangdong",
    "postalCode": "510623",
    "country": "CN"
  },
  "weight": { "value": 2.5, "unit": "Kg" },
  "sender": {
    "name": "张三",
    "phone": "13812345678",
    "email": "zhangsan@example.com"
  },
  "recipient": {
    "name": "李四",
    "phone": "13987654321",
    "email": "lisi@example.com"
  },
  "serviceLevel": "Express"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "carrierCode": "SF",
    "trackingNumber": "SF0000000001",
    "bookedAt": "2026-02-11T10:30:00+00:00"
  },
  "error": null,
  "correlationId": "..."
}
```

**Error (404 — unknown carrier):**
```json
{
  "success": false,
  "data": null,
  "error": { "code": "CARRIER_NOT_FOUND", "message": "No carrier adapter found for code 'XX'." },
  "correlationId": "..."
}
```

**curl:**
```bash
curl -X POST http://localhost:5198/api/carriers/SF/book \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "origin": {
      "street": "浦东新区陆家嘴环路1000号",
      "city": "上海",
      "province": "Shanghai",
      "postalCode": "200120",
      "country": "CN"
    },
    "destination": {
      "street": "天河区珠江新城花城大道18号",
      "city": "广州",
      "province": "Guangdong",
      "postalCode": "510623",
      "country": "CN"
    },
    "weight": { "value": 2.5, "unit": "Kg" },
    "sender": { "name": "张三", "phone": "13812345678", "email": "zhangsan@example.com" },
    "recipient": { "name": "李四", "phone": "13987654321", "email": "lisi@example.com" },
    "serviceLevel": "Express"
  }'
```

---

### 17. GET /api/carriers/{code}/track/{trackingNo}

Track a shipment by carrier code and tracking number.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Any authenticated user |

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `code` | string | Carrier code: `SF` or `JD` |
| `trackingNo` | string | Tracking number from booking |

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "trackingNumber": "SF0000000001",
    "status": "InTransit",
    "currentLocation": "Shanghai Distribution Center",
    "updatedAt": "2026-02-11T12:00:00+00:00"
  },
  "error": null,
  "correlationId": "..."
}
```

**curl:**
```bash
curl http://localhost:5198/api/carriers/SF/track/SF0000000001 \
  -H "Authorization: Bearer $TOKEN"
```

---

## Tracking

### 18. GET /api/tracking/{trackingNo}/snapshot

Get the latest tracking snapshot for a tracking number.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Any authenticated user |

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `trackingNo` | string | Tracking number (e.g. `SF0000000001`) |

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "trackingNumber": "SF0000000001",
    "currentStatus": "InTransit",
    "lastLocation": { "latitude": 31.2304, "longitude": 121.4737 },
    "updatedAt": "2026-02-11T12:00:00+00:00"
  },
  "error": null,
  "correlationId": "..."
}
```

**Error (404 — no tracking data):**
```json
{
  "success": false,
  "data": null,
  "error": { "code": "NOT_FOUND", "message": "No tracking data for SF0000000001" },
  "correlationId": "..."
}
```

**curl:**
```bash
curl http://localhost:5198/api/tracking/SF0000000001/snapshot \
  -H "Authorization: Bearer $TOKEN"
```

---

### 19. POST /api/tracking/{trackingNo}/subscribe

Subscribe to tracking updates for a tracking number (observer pattern).

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Any authenticated user |

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `trackingNo` | string | Tracking number to subscribe to |

**Request Body:** None

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "subscribed": true,
    "trackingNumber": "SF0000000001",
    "currentSnapshot": {
      "trackingNumber": "SF0000000001",
      "currentStatus": "InTransit",
      "lastLocation": { "latitude": 31.2304, "longitude": 121.4737 },
      "updatedAt": "2026-02-11T12:00:00+00:00"
    }
  },
  "error": null,
  "correlationId": "..."
}
```

**curl:**
```bash
curl -X POST http://localhost:5198/api/tracking/SF0000000001/subscribe \
  -H "Authorization: Bearer $TOKEN"
```

---

## Audit

### 20. GET /api/audit/entity/{entityType}/{entityId}

Get audit trail for a specific entity.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | `Admin`, `Dispatcher` |

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `entityType` | string | Entity type: `Order`, `Route`, `Carrier`, `Booking` |
| `entityId` | string | Entity identifier (GUID for orders) |

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": "audit-uuid",
      "entityType": "Order",
      "entityId": "b0000000-...",
      "action": "Created",
      "category": "DataChange",
      "actor": "admin",
      "correlationId": "order-create-001",
      "timestamp": "2026-02-11T10:00:00+00:00",
      "description": "Order DT-20260211-001 created",
      "payload": {
        "orderNumber": "DT-20260211-001",
        "customerName": "张三",
        "serviceLevel": "Express"
      }
    },
    {
      "id": "audit-uuid-2",
      "entityType": "Order",
      "entityId": "b0000000-...",
      "action": "StateChanged",
      "category": "StateTransition",
      "actor": "admin",
      "correlationId": "order-confirm-001",
      "timestamp": "2026-02-11T10:05:00+00:00",
      "description": "Order state changed: Created → Confirmed",
      "payload": null
    }
  ],
  "error": null,
  "correlationId": "..."
}
```

**Audit Actions:**

| Action | Category | Description |
|--------|----------|-------------|
| `Created` | DataChange | Entity was created |
| `Updated` | DataChange | Entity was modified |
| `Deleted` | DataChange | Entity was removed |
| `StateChanged` | StateTransition | Order state transition |
| `BusinessAction` | BusinessDecision | Business logic decision |

**curl:**
```bash
curl http://localhost:5198/api/audit/entity/Order/b0000000-0000-0000-0000-000000000001 \
  -H "Authorization: Bearer $TOKEN"
```

---

### 21. GET /api/audit/correlation/{correlationId}

Get all audit records sharing a correlation ID (end-to-end request trace).

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | `Admin`, `Dispatcher` |

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `correlationId` | string | Correlation ID from `X-Correlation-ID` header |

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": "audit-uuid",
      "entityType": "Order",
      "entityId": "b0000000-...",
      "action": "Created",
      "category": "DataChange",
      "actor": "admin",
      "correlationId": "order-create-001",
      "timestamp": "2026-02-11T10:00:00+00:00",
      "description": "Order DT-20260211-001 created",
      "payload": null
    }
  ],
  "error": null,
  "correlationId": "..."
}
```

**curl:**
```bash
curl http://localhost:5198/api/audit/correlation/order-create-001 \
  -H "Authorization: Bearer $TOKEN"
```

---

## Dashboard

### 22. GET /api/dashboard/stats

Get overall system statistics (total orders, revenue, status breakdown).

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Admin |

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "totalOrders": 15,
    "monthRevenue": 3500.00,
    "currency": "CNY",
    "statusBreakdown": {
      "Created": 3,
      "Confirmed": 5,
      "Shipped": 4,
      "Delivered": 2,
      "Cancelled": 1
    }
  }
}
```

---

### 23. GET /api/dashboard/carrier-performance

Get carrier performance metrics (on-time rate, average delivery time).

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Admin, Dispatcher |

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "carrierCode": "SF",
      "carrierName": "SF Express 顺丰速运",
      "totalShipments": 8,
      "onTimeRate": 92.5,
      "averageDeliveryHours": 36.2
    }
  ]
}
```

---

### 24. GET /api/dashboard/top-customers

Get top customers by order count.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Admin |

**Query Parameters:**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `limit` | int | 10 | Max number of customers to return |

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "customerName": "张三",
      "orderCount": 5,
      "totalSpent": 1200.00,
      "currency": "CNY"
    }
  ]
}
```

---

## Advanced Orders

### 25. POST /api/orders/bulk-create

Create multiple orders in a single request.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Admin, Dispatcher |

**Request Body:**
```json
{
  "orders": [
    {
      "customerName": "Bulk Customer A",
      "customerPhone": "13900001001",
      "customerEmail": "bulk-a@example.com",
      "origin": { "street": "10 Warehouse Rd", "city": "Toronto", "province": "ON", "postalCode": "M5V 1A1", "country": "CA" },
      "destination": { "street": "20 Delivery St", "city": "Ottawa", "province": "ON", "postalCode": "K1A 0B1", "country": "CA" },
      "serviceLevel": "Express",
      "items": [
        { "description": "Widget X", "weight": { "value": 1.0, "unit": "kg" }, "quantity": 5 }
      ]
    }
  ]
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "successCount": 2,
    "failureCount": 0,
    "results": [
      { "success": true, "orderId": "...", "orderNumber": "ORD-20260215-XXXX" }
    ]
  }
}
```

---

### 26. PUT /api/orders/{id}/update-destination

Update the destination address for an existing order (before shipping).

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Admin, Dispatcher |

**Request Body:**
```json
{
  "destination": {
    "street": "999 New Delivery Blvd",
    "city": "Edmonton",
    "province": "AB",
    "postalCode": "T5J 0N3",
    "country": "CA"
  }
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "orderId": "...",
    "newDestination": "999 New Delivery Blvd, Edmonton, AB",
    "status": "Created"
  }
}
```

---

### 27. POST /api/orders/{id}/split-shipment

Split an order into multiple sub-orders by item groups.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Admin, Dispatcher |

**Request Body:**
```json
{
  "groups": [[0], [1, 2]]
}
```
Each inner array contains zero-based item indices to group together.

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "originalOrderId": "...",
    "newOrders": [
      { "orderId": "...", "orderNumber": "ORD-20260215-XXXX", "itemCount": 1 },
      { "orderId": "...", "orderNumber": "ORD-20260215-YYYY", "itemCount": 2 }
    ]
  }
}
```

---

## Webhooks

### 28. POST /api/webhooks/carrier/{code}

Receive tracking status updates from external carrier systems.

| Property | Value |
|----------|-------|
| **Auth Required** | ❌ No (anonymous) |
| **Security** | HMAC-SHA256 signature via `X-Webhook-Signature` header |

**Security:** Compute `HMAC-SHA256(requestBody, secret)` and send as:
```
X-Webhook-Signature: sha256={hex-encoded-hash}
```
Secret is configured via `Webhooks:Secret` in appsettings.json (default: `dt-express-webhook-secret-2026`).

**Request Body:**
```json
{
  "trackingNumber": "SF0000000001",
  "status": "InTransit",
  "description": "Package arrived at sorting center",
  "latitude": 31.2304,
  "longitude": 121.4737,
  "occurredAt": "2026-01-15T10:30:00Z"
}
```

**Valid Statuses:** `Created`, `PickedUp`, `InTransit`, `OutForDelivery`, `Delivered`, `Exception`

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "accepted": true,
    "trackingNumber": "SF0000000001",
    "status": "InTransit",
    "carrierCode": "SF"
  }
}
```

**Error Responses:**
- `401 Unauthorized` — Missing or invalid HMAC signature
- `400 Bad Request` — Invalid payload or status value

---

## Reports

### 29. GET /api/reports/shipments/monthly

Get monthly shipment report with optional CSV export.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Admin, Dispatcher |

**Query Parameters:**

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `month` | string | ✅ | Month in `YYYY-MM` format (e.g. `2026-01`) |
| `format` | string | ❌ | Output format: `json` (default) or `csv` |

**Response (200 OK — JSON):**
```json
{
  "success": true,
  "data": {
    "year": 2026,
    "month": 1,
    "totalShipments": 12,
    "totalRevenue": 5600.00,
    "currency": "CNY",
    "shipments": [
      {
        "orderId": "...",
        "orderNumber": "ORD-20260115-XXXX",
        "customerName": "张三",
        "origin": "上海, 上海市",
        "destination": "北京, 北京市",
        "status": "Delivered",
        "serviceLevel": "Express",
        "carrierCode": "SF",
        "trackingNumber": "SF0000000001",
        "cost": 280.50,
        "costCurrency": "CNY",
        "createdAt": "2026-01-15T10:00:00+08:00"
      }
    ]
  }
}
```

**Response (200 OK — CSV):**
Returns a downloadable CSV file with header:
```
OrderId,OrderNumber,CustomerName,Origin,Destination,Status,ServiceLevel,CarrierCode,TrackingNumber,Cost,Currency,CreatedAt
```

---

### 30. GET /api/reports/revenue/by-carrier

Get revenue breakdown by carrier for a date range.

| Property | Value |
|----------|-------|
| **Auth Required** | ✅ Yes |
| **Roles** | Admin |

**Query Parameters:**

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `from` | string | ✅ | Start date (inclusive), ISO 8601 (e.g. `2026-01-01`) |
| `to` | string | ✅ | End date (exclusive), ISO 8601 (e.g. `2026-01-31`) |

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "fromDate": "2026-01-01T00:00:00+00:00",
    "toDate": "2026-01-31T00:00:00+00:00",
    "grandTotal": 12800.00,
    "currency": "CNY",
    "carriers": [
      {
        "carrierCode": "SF",
        "carrierName": "SF Express 顺丰速运",
        "orderCount": 8,
        "totalRevenue": 7200.00,
        "averageOrderValue": 900.00,
        "percentageOfTotal": 56.3
      },
      {
        "carrierCode": "JD",
        "carrierName": "JD Logistics 京东物流",
        "orderCount": 5,
        "totalRevenue": 5600.00,
        "averageOrderValue": 1120.00,
        "percentageOfTotal": 43.7
      }
    ]
  }
}
```

---

## Real-Time — SignalR Hub

### SignalR Tracking Hub

Real-time tracking event streaming via WebSocket (SignalR).

| Property | Value |
|----------|-------|
| **URL** | `ws://localhost:5198/hubs/tracking?access_token={jwt}` |
| **Auth Required** | ✅ Yes (JWT via query string) |
| **Roles** | Any authenticated user |
| **Protocol** | SignalR (WebSocket transport preferred) |

**Connection:**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5198/hubs/tracking", {
        accessTokenFactory: () => "your-jwt-token"
    })
    .build();
```

**Server Methods (client → server):**

| Method | Parameters | Description |
|--------|-----------|-------------|
| `SubscribeTracking` | `trackingNo: string` | Join tracking number group. Returns current snapshot immediately. |
| `UnsubscribeTracking` | `trackingNo: string` | Leave tracking number group. |

**Client Events (server → client):**

| Event | Payload | Trigger |
|-------|---------|---------|
| `TrackingUpdate` | `TrackingEventDto` | When a tracking event occurs (e.g. webhook received) |
| `TrackingSnapshot` | `TrackingSnapshotDto` | Immediately on `SubscribeTracking` (current state) |

**TrackingEventDto:**
```json
{
  "trackingNumber": "SF0000000001",
  "eventType": "StatusChanged",
  "newStatus": "InTransit",
  "location": { "latitude": 31.2304, "longitude": 121.4737 },
  "description": "Package arrived at sorting center",
  "occurredAt": "2026-01-15T10:30:00Z"
}
```

**TrackingSnapshotDto:**
```json
{
  "trackingNumber": "SF0000000001",
  "currentStatus": "InTransit",
  "lastLocation": { "latitude": 31.2304, "longitude": 121.4737 },
  "updatedAt": "2026-01-15T10:30:00Z"
}
```

---

## Endpoint Summary Table

| # | Method | Path | Auth | Roles | Description |
|---|--------|------|------|-------|-------------|
| 1 | POST | `/api/auth/login` | ❌ | Any | Login |
| 2 | POST | `/api/auth/register` | ❌ | Any | Register |
| 3 | POST | `/api/auth/refresh` | ❌ | Any | Refresh token |
| 4 | POST | `/api/orders` | ✅ | Admin, Dispatcher | Create order |
| 5 | GET | `/api/orders` | ✅ | Any | List orders |
| 6 | GET | `/api/orders/{id}` | ✅ | Any | Get order detail |
| 7 | PUT | `/api/orders/{id}/confirm` | ✅ | Admin, Dispatcher | Confirm order |
| 8 | PUT | `/api/orders/{id}/ship` | ✅ | Admin, Dispatcher | Ship order |
| 9 | PUT | `/api/orders/{id}/deliver` | ✅ | Admin, Driver | Deliver order |
| 10 | PUT | `/api/orders/{id}/cancel` | ✅ | Admin, Dispatcher | Cancel order |
| 11 | POST | `/api/routing/calculate` | ✅ | Any | Calculate route |
| 12 | POST | `/api/routing/compare` | ✅ | Any | Compare strategies |
| 13 | GET | `/api/routing/strategies` | ✅ | Any | List strategies |
| 14 | GET | `/api/carriers` | ❌ | Any | List carriers |
| 15 | POST | `/api/carriers/quotes` | ✅ | Any | Get quotes |
| 16 | POST | `/api/carriers/{code}/book` | ✅ | Admin, Dispatcher | Book shipment |
| 17 | GET | `/api/carriers/{code}/track/{no}` | ✅ | Any | Track shipment |
| 18 | GET | `/api/tracking/{no}/snapshot` | ✅ | Any | Tracking snapshot |
| 19 | POST | `/api/tracking/{no}/subscribe` | ✅ | Any | Subscribe tracking |
| 20 | GET | `/api/audit/entity/{type}/{id}` | ✅ | Admin, Dispatcher | Audit by entity |
| 21 | GET | `/api/audit/correlation/{id}` | ✅ | Admin, Dispatcher | Audit by correlation |
| 22 | GET | `/api/dashboard/stats` | ✅ | Admin | Dashboard stats |
| 23 | GET | `/api/dashboard/carrier-performance` | ✅ | Admin, Dispatcher | Carrier performance |
| 24 | GET | `/api/dashboard/top-customers` | ✅ | Admin | Top customers |
| 25 | POST | `/api/orders/bulk-create` | ✅ | Admin, Dispatcher | Bulk create orders |
| 26 | PUT | `/api/orders/{id}/update-destination` | ✅ | Admin, Dispatcher | Update destination |
| 27 | POST | `/api/orders/{id}/split-shipment` | ✅ | Admin, Dispatcher | Split shipment |
| 28 | POST | `/api/webhooks/carrier/{code}` | HMAC | Anonymous (HMAC-SHA256) | Carrier webhook |
| 29 | GET | `/api/reports/shipments/monthly` | ✅ | Admin, Dispatcher | Monthly shipment report |
| 30 | GET | `/api/reports/revenue/by-carrier` | ✅ | Admin | Revenue by carrier |
| — | WS | `/hubs/tracking` | ✅ | Any | SignalR real-time tracking |

---

## Order State Machine

```
                  ┌──────────┐
                  │ Created  │
                  └────┬─────┘
                       │ confirm
                  ┌────▼─────┐
          ┌───────│ Confirmed│
          │       └────┬─────┘
   cancel │            │ ship
          │       ┌────▼─────┐
          │       │ Shipped  │
          │       └────┬─────┘
          │            │ deliver
          │       ┌────▼─────┐
          │       │ Delivered│  (terminal)
          │       └──────────┘
          │
     ┌────▼─────┐
     │ Cancelled │  (terminal)
     └──────────┘
```

**Valid Transitions:**
| From | To | Action | Roles |
|------|----|--------|-------|
| Created | Confirmed | `confirm` | Admin, Dispatcher |
| Created | Cancelled | `cancel` | Admin, Dispatcher |
| Confirmed | Shipped | `ship` | Admin, Dispatcher |
| Confirmed | Cancelled | `cancel` | Admin, Dispatcher |
| Shipped | Delivered | `deliver` | Admin, Driver |
