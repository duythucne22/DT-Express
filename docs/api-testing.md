# DT-Express API Testing Guide

## Overview

Comprehensive integration test suite covering JWT authentication, order lifecycle, database relationships, endpoint security (RBAC), and edge cases.

**Test framework**: xUnit + `WebApplicationFactory` (in-memory EF Core)

---

## How to Run Tests

### Run all tests
```bash
dotnet test tests/DtExpress.Api.Tests/
```

### Run a specific test class
```bash
dotnet test tests/DtExpress.Api.Tests/ --filter "FullyQualifiedName~AuthenticationTests"
dotnet test tests/DtExpress.Api.Tests/ --filter "FullyQualifiedName~OrderLifecycleTests"
dotnet test tests/DtExpress.Api.Tests/ --filter "FullyQualifiedName~EndpointSecurityTests"
dotnet test tests/DtExpress.Api.Tests/ --filter "FullyQualifiedName~DatabaseRelationshipTests"
dotnet test tests/DtExpress.Api.Tests/ --filter "FullyQualifiedName~EdgeCaseTests"
```

### Run with verbose output
```bash
dotnet test tests/DtExpress.Api.Tests/ --verbosity normal
```

### Run with code coverage
```bash
dotnet test tests/DtExpress.Api.Tests/ --collect:"XPlat Code Coverage"
```

---

## Test Accounts (Seeded)

| Username    | Password      | Role       | Permissions                         |
|-------------|---------------|------------|-------------------------------------|
| `admin`     | `admin123`    | Admin      | Full access to all endpoints        |
| `dispatcher`| `passwd123`   | Dispatcher | Create/confirm/ship/cancel orders   |
| `driver`    | `passwd123`   | Driver     | Deliver orders, read access         |
| `viewer`    | `passwd123`   | Viewer     | Read-only access                    |

---

## Test Suites

### 1. AuthenticationTests (14 tests)
- ✅ Login with valid credentials for all 4 roles
- ✅ Token refresh mechanism (single-use enforcement)
- ❌ Wrong password → 400 `AUTH_FAILED`
- ❌ Nonexistent user → 400 `AUTH_FAILED`
- ❌ Invalid refresh token → 400 `INVALID_REFRESH_TOKEN`
- ❌ Malformed JWT → 401
- ❌ Missing token → 401
- ✅ User registration with tokens
- ❌ Duplicate username → 400 `USERNAME_TAKEN`
- ❌ Invalid role → 400 `INVALID_ROLE`
- ✅ Correlation ID in auth responses

### 2. OrderLifecycleTests (6 tests)
- ✅ Full lifecycle: Created → Confirmed → Shipped → Delivered
- ✅ Cancel order from Created state
- ✅ List orders with status filter
- ✅ List all orders
- ✅ Correlation ID preserved through lifecycle
- ✅ Audit trail verification (≥4 records per lifecycle)

### 3. DatabaseRelationshipTests (7 tests)
- ✅ Order → OrderItems cascade (multiple items)
- ✅ Weight and Dimension persistence
- ✅ State transitions generate audit records
- ✅ Audit query by correlation ID
- ✅ Direct DbContext verification
- ✅ Seed user verification
- ✅ Audit entity type and ID correctness

### 4. EndpointSecurityTests (28 tests)
- All 18 endpoints tested for auth/RBAC:
  - ✅ Anonymous endpoints work without token
  - ❌ Protected endpoints return 401 without token
  - ❌ Role-restricted endpoints return 403 with wrong role
  - ✅ Correct roles get 200/201
- ✅ ApiResponse envelope structure verification
- ✅ Correlation ID presence

### 5. EdgeCaseTests (12 tests)
- ❌ Invalid state transitions return 400 `INVALID_TRANSITION`
- ✅ Large order with 12 items
- ✅ Concurrent order creation (5 parallel)
- ❌ Nonexistent order → 404
- ❌ Unknown carrier → 404 `CARRIER_NOT_FOUND`
- ❌ Unknown strategy → 400 `STRATEGY_NOT_FOUND`
- ❌ Cancel shipped/delivered/cancelled orders → 400

---

## Sample API Calls (curl)

### Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin123"}'
```

**Expected response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "base64string...",
    "expiresAt": "2026-02-11T16:00:00Z",
    "userId": "a0000000-0000-0000-0000-000000000001",
    "username": "admin",
    "displayName": "系统管理员",
    "role": "Admin"
  },
  "correlationId": "abc-123"
}
```

### Create Order (requires Admin or Dispatcher)
```bash
TOKEN="eyJhbGciOiJIUzI1NiIs..."

curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Correlation-ID: my-trace-001" \
  -d '{
    "customerName": "张三",
    "customerPhone": "13812345678",
    "customerEmail": "zhangsan@example.com",
    "origin": {
      "street": "浦东新区陆家嘴环路1000号",
      "city": "上海",
      "province": "Shanghai",
      "postalCode": "200120"
    },
    "destination": {
      "street": "天河区珠江新城花城大道18号",
      "city": "广州",
      "province": "Guangdong",
      "postalCode": "510623"
    },
    "serviceLevel": "Express",
    "items": [{
      "description": "电子产品 - 笔记本电脑",
      "quantity": 1,
      "weight": { "value": 2.5, "unit": "Kg" },
      "dimensions": { "lengthCm": 35, "widthCm": 25, "heightCm": 3 }
    }]
  }'
```

**Expected response (201):**
```json
{
  "success": true,
  "data": {
    "orderId": "guid-here",
    "orderNumber": "DT-20260211-001",
    "status": "Created"
  },
  "correlationId": "my-trace-001"
}
```

### Confirm Order
```bash
curl -X PUT http://localhost:5000/api/orders/{orderId}/confirm \
  -H "Authorization: Bearer $TOKEN"
```

### Ship Order
```bash
curl -X PUT http://localhost:5000/api/orders/{orderId}/ship \
  -H "Authorization: Bearer $TOKEN"
```

**Expected response (200):**
```json
{
  "success": true,
  "data": {
    "orderId": "guid-here",
    "newStatus": "Shipped",
    "carrierCode": "JD",
    "trackingNumber": "JD1234567890"
  }
}
```

### Deliver Order (requires Admin or Driver)
```bash
curl -X PUT http://localhost:5000/api/orders/{orderId}/deliver \
  -H "Authorization: Bearer $TOKEN"
```

### Cancel Order
```bash
curl -X PUT http://localhost:5000/api/orders/{orderId}/cancel \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason": "客户取消"}'
```

### Get Carrier Quotes
```bash
curl -X POST http://localhost:5000/api/carriers/quotes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "origin": {"street": "浦东新区", "city": "上海", "province": "Shanghai", "postalCode": "200120"},
    "destination": {"street": "天河区", "city": "广州", "province": "Guangdong", "postalCode": "510623"},
    "weight": {"value": 2.5, "unit": "Kg"},
    "serviceLevel": "Express"
  }'
```

### Calculate Route
```bash
curl -X POST http://localhost:5000/api/routing/calculate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "origin": {"latitude": 31.2304, "longitude": 121.4737},
    "destination": {"latitude": 39.9042, "longitude": 116.4074},
    "packageWeight": {"value": 2.5, "unit": "Kg"},
    "serviceLevel": "Express",
    "strategy": "Fastest"
  }'
```

### Get Audit Trail
```bash
curl http://localhost:5000/api/audit/entity/Order/{orderId} \
  -H "Authorization: Bearer $TOKEN"
```

---

## Role-Based Access Control (RBAC) Matrix

| Endpoint | Admin | Dispatcher | Driver | Viewer | Anonymous |
|---|:---:|:---:|:---:|:---:|:---:|
| `POST /api/auth/login,register,refresh` | — | — | — | — | ✅ |
| `GET /api/carriers` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `POST /api/carriers/quotes` | ✅ | ✅ | ✅ | ✅ | ❌ 401 |
| `POST /api/carriers/{code}/book` | ✅ | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| `GET /api/carriers/{code}/track/{no}` | ✅ | ✅ | ✅ | ✅ | ❌ 401 |
| `POST /api/routing/calculate` | ✅ | ✅ | ✅ | ✅ | ❌ 401 |
| `POST /api/routing/compare` | ✅ | ✅ | ✅ | ✅ | ❌ 401 |
| `GET /api/routing/strategies` | ✅ | ✅ | ✅ | ✅ | ❌ 401 |
| `GET /api/tracking/{no}/snapshot` | ✅ | ✅ | ✅ | ✅ | ❌ 401 |
| `POST /api/tracking/{no}/subscribe` | ✅ | ✅ | ✅ | ✅ | ❌ 401 |
| `POST /api/orders` (create) | ✅ | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| `GET /api/orders` (list) | ✅ | ✅ | ✅ | ✅ | ❌ 401 |
| `GET /api/orders/{id}` | ✅ | ✅ | ✅ | ✅ | ❌ 401 |
| `PUT /api/orders/{id}/confirm` | ✅ | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| `PUT /api/orders/{id}/ship` | ✅ | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| `PUT /api/orders/{id}/deliver` | ✅ | ❌ 403 | ✅ | ❌ 403 | ❌ 401 |
| `PUT /api/orders/{id}/cancel` | ✅ | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| `GET /api/audit/entity/{type}/{id}` | ✅ | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| `GET /api/audit/correlation/{id}` | ✅ | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |

---

## Error Response Format

All errors follow the `ApiResponse<T>` envelope:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "INVALID_TRANSITION",
    "message": "Cannot apply action Ship in state Created."
  },
  "correlationId": "abc-123"
}
```

### Error Codes

| Code | HTTP Status | Meaning |
|------|:-----------:|---------|
| `AUTH_FAILED` | 400 | Invalid username or password |
| `ACCOUNT_DISABLED` | 400 | User account deactivated |
| `INVALID_REFRESH_TOKEN` | 400 | Refresh token invalid or consumed |
| `USERNAME_TAKEN` | 400 | Registration: duplicate username |
| `EMAIL_TAKEN` | 400 | Registration: duplicate email |
| `INVALID_ROLE` | 400 | Registration: role not in valid set |
| `INVALID_TRANSITION` | 400 | State Pattern: action not valid for current state |
| `CARRIER_NOT_FOUND` | 404 | Unknown carrier code |
| `STRATEGY_NOT_FOUND` | 400 | Unknown routing strategy name |
| `VALIDATION_ERROR` | 400 | General argument/input validation failure |
| `INTERNAL_ERROR` | 500 | Unhandled server error |

---

## Architecture Notes

- **Test isolation**: Each `CustomWebApplicationFactory` instance gets a unique in-memory database
- **No Docker required**: Tests use EF InMemory provider (no PostgreSQL needed)
- **Seeded data**: 4 test users auto-seeded with BCrypt hashes on factory creation
- **Correlation tracing**: All tests verify `X-Correlation-ID` header and `ApiResponse.CorrelationId`
