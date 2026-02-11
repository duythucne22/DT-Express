# DT-Express TMS — Database Schema Documentation

> **Database**: PostgreSQL 15+  
> **Encoding**: UTF-8 (full Chinese character support)  
> **Naming**: snake_case (PostgreSQL convention)  
> **ORM**: EF Core 8.0 with Fluent API  
> **Schema file**: [database/schema.sql](../database/schema.sql)  
> **Seed data**: [database/seed-data.sql](../database/seed-data.sql)

---

## Table of Contents

1. [ER Diagram](#er-diagram)
2. [Table Overview](#table-overview)
3. [Table Details](#table-details)
4. [Index Strategy](#index-strategy)
5. [Value Object Mapping](#value-object-mapping)
6. [Sample Queries](#sample-queries)

---

## ER Diagram

```
┌──────────────┐         ┌──────────────┐
│    users     │         │   carriers   │
│──────────────│         │──────────────│
│ id (PK,UUID) │◄──┐     │ code (PK)    │◄──────────┐
│ username (UQ)│   │     │ name         │           │
│ email (UQ)   │   │     │ is_active    │           │
│ password_hash│   │     │ created_at   │           │
│ display_name │   │     └──────────────┘           │
│ role         │   │           │                    │
│ is_active    │   │           │ 1:N                │
│ created_at   │   │           │                    │
│ updated_at   │   │     ┌─────▼────────┐           │
└──────────────┘   │     │    orders    │           │
       │           │     │──────────────│           │
       │ 1:N       ├─────│ user_id (FK) │           │
       │           │     │ carrier_code │───────────┘
       │           │     │ (FK)         │
       │           │     │ id (PK,UUID) │◄─────────────────┐
       │           │     │ order_number │                  │
       │           │     │ customer_*   │                  │
       │           │     │ origin_*     │                  │
       │           │     │ dest_*       │                  │
       │           │     │ service_level│                  │
       │           │     │ status       │                  │
       │           │     │ tracking_no  │                  │
       │           │     │ created_at   │                  │
       │           │     │ updated_at   │                  │
       │           │     └──────┬───────┘                  │
       │           │            │                          │
       │           │     ┌──────┼──────────┐               │
       │           │     │      │          │               │
       │           │     │ 1:N  │ 1:N      │ 1:N           │
       │           │     │      │          │               │
       │           │ ┌───▼──┐ ┌─▼────┐ ┌──▼──────┐   ┌─────▼──────┐
       │           │ │order_│ │order_│ │bookings │   │carrier_    │
       │           │ │items │ │events│ │─────────│   │quotes      │
       │           │ │──────│ │──────│ │id (PK)  │   │────────────│
       │           │ │id    │ │id    │ │order_id │   │id (PK)     │
       │           │ │ord_id│ │ord_id│ │carrier_ │   │order_id(FK)│
       │           │ │desc  │ │prev_ │ │  code   │   │carrier_code│
       │           │ │qty   │ │  stat│ │tracking_│   │price_*     │
       │           │ │wt_*  │ │new_  │ │  number │   │est_days    │
       │           │ │dim_* │ │  stat│ │  (UQ)   │   │selected    │
       │           │ └──────┘ │action│ │booked_at│   └────────────┘
       │           │          │occ_at│ └────┬────┘
       │           │          └──────┘      │
       │           │                   ┌────────────────┐
       │           │                   │                │
       │           │                   │ 1:N            │ 1:1
       │           │                   │                │
       │           │            ┌──────▼──┐  ┌──────────▼───┐
       │           │            │tracking_│  │tracking_     │
       │           │            │events   │  │snapshots     │
       │           │            │─────────│  │──────────────│
       │           │            │id (PK)  │  │tracking_no   │
       │           │            │track_no │  │  (PK,FK)     │
       │           │            │event_typ│  │current_status│
       │           │            │new_stat │  │last_loc_*    │
       │           │            │loc_*    │  │updated_at    │
       │           │            │desc     │  └──────────────┘
       │           │            │occ_at   │
       │           │            └─────────┘
       │           │
       │      ┌────▼─────────┐
       │      │  audit_logs  │
       │      │──────────────│
       └──────│actor_user_id │
              │  (FK)        │
              │ id (PK,UUID) │
              │ entity_type  │
              │ entity_id    │
              │ action       │
              │ category     │
              │ actor_name   │
              │ correlation  │
              │ timestamp    │
              │ description  │
              │ payload      │
              └──────────────┘
```

---

## Table Overview

| # | Table | Rows (seed) | Description | .NET Entity |
|---|-------|-------------|-------------|-------------|
| 1 | `users` | 4 | System users with BCrypt auth | `UserEntity` |
| 2 | `carriers` | 2 | Reference: SF Express, JD Logistics | `CarrierEntity` |
| 3 | `orders` | 1 | Order aggregate root (value objects flattened) | `OrderEntity` |
| 4 | `order_items` | 2 | Line items owned by order (cascade delete) | `OrderItemEntity` |
| 5 | `order_events` | 0 | State machine transition log | `OrderEventEntity` |
| 6 | `bookings` | 0 | Carrier booking records | `BookingEntity` |
| 7 | `tracking_events` | 0 | Append-only tracking event stream | `TrackingEventEntity` |
| 8 | `tracking_snapshots` | 0 | Materialized latest tracking state | `TrackingSnapshotEntity` |
| 9 | `audit_logs` | 1 | Immutable audit trail (JSONB payload) | `AuditLogEntity` |
| 10 | `carrier_quotes` | 0 | Quote comparison history | `CarrierQuoteEntity` |

---

## Table Details

### 1. users

System users for JWT authentication.

| Column | Type | Nullable | Default | .NET Type | Description |
|--------|------|----------|---------|-----------|-------------|
| `id` | UUID | NO | `gen_random_uuid()` | `Guid` | Primary key |
| `username` | VARCHAR(100) | NO | — | `string` | Unique login name |
| `email` | VARCHAR(200) | NO | — | `string` | Unique email |
| `password_hash` | VARCHAR(200) | NO | — | `string` | BCrypt hash (work factor 12) |
| `display_name` | VARCHAR(200) | NO | — | `string` | Chinese display name (系统管理员) |
| `role` | VARCHAR(50) | NO | — | `string` | CHECK: Admin, Dispatcher, Driver, Viewer |
| `is_active` | BOOLEAN | NO | `TRUE` | `bool` | Account enabled flag |
| `created_at` | TIMESTAMPTZ | NO | `NOW()` | `DateTimeOffset` | Creation timestamp |
| `updated_at` | TIMESTAMPTZ | NO | `NOW()` | `DateTimeOffset` | Last update timestamp |

**Seeded accounts:**

| Username | Role | Display Name | Password |
|----------|------|-------------|----------|
| admin | Admin | 系统管理员 | admin123 |
| dispatcher | Dispatcher | 调度员小李 | passwd123 |
| driver | Driver | 司机王师傅 | passwd123 |
| viewer | Viewer | 客服张小姐 | passwd123 |

---

### 2. carriers

Reference data for carrier integrations.

| Column | Type | Nullable | Default | .NET Type | Description |
|--------|------|----------|---------|-----------|-------------|
| `code` | VARCHAR(10) | NO | — | `string` | Primary key ("SF", "JD") |
| `name` | VARCHAR(100) | NO | — | `string` | Display name (顺丰速运, 京东物流) |
| `is_active` | BOOLEAN | NO | `TRUE` | `bool` | Carrier enabled |
| `created_at` | TIMESTAMPTZ | NO | `NOW()` | `DateTimeOffset` | Created |

---

### 3. orders

Order aggregate root with flattened value objects.

| Column | Type | Nullable | Default | .NET Type | Description |
|--------|------|----------|---------|-----------|-------------|
| `id` | UUID | NO | `gen_random_uuid()` | `Guid` | Primary key |
| `order_number` | VARCHAR(50) | NO | — | `string` | Business key (DT-YYYYMMDD-NNN) |
| `customer_name` | VARCHAR(200) | NO | — | `string` | ContactInfo.Name (张三) |
| `customer_phone` | VARCHAR(20) | NO | — | `string` | ContactInfo.Phone (13812345678) |
| `customer_email` | VARCHAR(200) | YES | — | `string?` | ContactInfo.Email |
| `origin_street` | VARCHAR(300) | NO | — | `string` | Address.Street |
| `origin_city` | VARCHAR(100) | NO | — | `string` | Address.City (上海) |
| `origin_province` | VARCHAR(50) | NO | — | `string` | Address.Province |
| `origin_postal_code` | VARCHAR(10) | NO | — | `string` | 6-digit CN postal code |
| `origin_country` | VARCHAR(5) | NO | `'CN'` | `string` | ISO country code |
| `dest_street` | VARCHAR(300) | NO | — | `string` | Destination street |
| `dest_city` | VARCHAR(100) | NO | — | `string` | Destination city (广州) |
| `dest_province` | VARCHAR(50) | NO | — | `string` | Destination province |
| `dest_postal_code` | VARCHAR(10) | NO | — | `string` | 6-digit postal code |
| `dest_country` | VARCHAR(5) | NO | `'CN'` | `string` | ISO country code |
| `service_level` | VARCHAR(20) | NO | — | `string` | Express, Standard, Economy |
| `status` | VARCHAR(20) | NO | `'Created'` | `string` | Order state machine status |
| `tracking_number` | VARCHAR(100) | YES | — | `string?` | Set after carrier booking |
| `carrier_code` | VARCHAR(10) | YES | — | `string?` | FK → carriers(code) |
| `user_id` | UUID | YES | — | `Guid?` | FK → users(id) |
| `created_at` | TIMESTAMPTZ | NO | `NOW()` | `DateTimeOffset` | Order creation time |
| `updated_at` | TIMESTAMPTZ | NO | `NOW()` | `DateTimeOffset` | Last update time |

**FK Relationships:**
- `carrier_code` → `carriers(code)` ON DELETE SET NULL
- `user_id` → `users(id)` ON DELETE SET NULL

---

### 4. order_items

Line items owned by an order (cascade delete).

| Column | Type | Nullable | Default | .NET Type | Description |
|--------|------|----------|---------|-----------|-------------|
| `id` | UUID | NO | `gen_random_uuid()` | `Guid` | Primary key |
| `order_id` | UUID | NO | — | `Guid` | FK → orders(id) CASCADE |
| `description` | VARCHAR(500) | NO | — | `string` | Item description (电子产品) |
| `quantity` | INTEGER | NO | — | `int` | CHECK: > 0 |
| `weight_value` | DECIMAL(10,3) | NO | — | `decimal` | Weight.Value |
| `weight_unit` | VARCHAR(5) | NO | — | `string` | Kg, G, Jin, Lb |
| `dim_length_cm` | DECIMAL(10,2) | YES | — | `decimal?` | Dimension.LengthCm |
| `dim_width_cm` | DECIMAL(10,2) | YES | — | `decimal?` | Dimension.WidthCm |
| `dim_height_cm` | DECIMAL(10,2) | YES | — | `decimal?` | Dimension.HeightCm |

**Constraints:**
- `chk_dimensions_all_or_none`: All dimension columns must be NULL together or all non-NULL and > 0
- Cascade delete: when an order is deleted, its items are automatically removed

---

### 5. order_events

State machine transition history (Event Sourcing Lite).

| Column | Type | Nullable | Default | .NET Type | Description |
|--------|------|----------|---------|-----------|-------------|
| `id` | UUID | NO | `gen_random_uuid()` | `Guid` | Primary key |
| `order_id` | UUID | NO | — | `Guid` | FK → orders(id) CASCADE |
| `previous_status` | VARCHAR(20) | NO | — | `string` | State before transition |
| `new_status` | VARCHAR(20) | NO | — | `string` | State after transition |
| `action` | VARCHAR(20) | NO | — | `string` | Confirm, Ship, Deliver, Cancel |
| `occurred_at` | TIMESTAMPTZ | NO | — | `DateTimeOffset` | When the transition occurred |

---

### 6. bookings

Carrier booking records linking orders to carriers.

| Column | Type | Nullable | Default | .NET Type | Description |
|--------|------|----------|---------|-----------|-------------|
| `id` | UUID | NO | `gen_random_uuid()` | `Guid` | Primary key |
| `order_id` | UUID | YES | — | `Guid?` | FK → orders(id) SET NULL |
| `carrier_code` | VARCHAR(10) | NO | — | `string` | FK → carriers(code) RESTRICT |
| `tracking_number` | VARCHAR(100) | NO | — | `string` | UNIQUE — FK target for tracking |
| `booked_at` | TIMESTAMPTZ | NO | — | `DateTimeOffset` | Booking timestamp |

---

### 7. tracking_events

Append-only event stream for real-time tracking.

| Column | Type | Nullable | Default | .NET Type | Description |
|--------|------|----------|---------|-----------|-------------|
| `id` | UUID | NO | `gen_random_uuid()` | `Guid` | Primary key |
| `tracking_number` | VARCHAR(100) | NO | — | `string` | FK → bookings(tracking_number) CASCADE |
| `event_type` | VARCHAR(30) | NO | — | `string` | StatusChanged, LocationUpdated |
| `new_status` | VARCHAR(30) | YES | — | `string?` | ShipmentStatus enum (null for LocationUpdated) |
| `location_lat` | DECIMAL(9,6) | YES | — | `decimal?` | GeoCoordinate.Latitude |
| `location_lng` | DECIMAL(9,6) | YES | — | `decimal?` | GeoCoordinate.Longitude |
| `description` | TEXT | YES | — | `string?` | Event description |
| `occurred_at` | TIMESTAMPTZ | NO | — | `DateTimeOffset` | When the event occurred |

**Constraints:**
- `chk_location_pair`: lat/lng must both be NULL or both be non-NULL

---

### 8. tracking_snapshots

Materialized latest state (one per shipment, UPSERT pattern).

| Column | Type | Nullable | Default | .NET Type | Description |
|--------|------|----------|---------|-----------|-------------|
| `tracking_number` | VARCHAR(100) | NO | — | `string` | PK + FK → bookings(tracking_number) CASCADE |
| `current_status` | VARCHAR(30) | NO | — | `string` | Latest ShipmentStatus |
| `last_location_lat` | DECIMAL(9,6) | YES | — | `decimal?` | Latest latitude |
| `last_location_lng` | DECIMAL(9,6) | YES | — | `decimal?` | Latest longitude |
| `updated_at` | TIMESTAMPTZ | NO | — | `DateTimeOffset` | Last event timestamp |

---

### 9. audit_logs

Immutable audit trail (append-only, never updated/deleted).

| Column | Type | Nullable | Default | .NET Type | Description |
|--------|------|----------|---------|-----------|-------------|
| `id` | UUID | NO | `gen_random_uuid()` | `Guid` | Primary key |
| `entity_type` | VARCHAR(50) | NO | — | `string` | "Order", "Booking", "Route" |
| `entity_id` | VARCHAR(100) | NO | — | `string` | Entity identifier |
| `action` | VARCHAR(50) | NO | — | `string` | Created, Updated, Deleted, StateChanged, BusinessAction |
| `category` | VARCHAR(50) | NO | — | `string` | DataChange, StateTransition, ExternalCall, BusinessDecision |
| `actor_user_id` | UUID | YES | — | `Guid?` | FK → users(id) SET NULL |
| `actor_name` | VARCHAR(200) | NO | — | `string` | Text snapshot (survives user deletion) |
| `correlation_id` | VARCHAR(100) | NO | — | `string` | Distributed tracing ID |
| `timestamp` | TIMESTAMPTZ | NO | — | `DateTimeOffset` | When the action occurred |
| `description` | TEXT | YES | — | `string?` | Human-readable description |
| `payload` | JSONB | YES | — | `Dictionary<string, object?>?` | Before/after diffs, extra data |
| `created_at` | TIMESTAMPTZ | NO | `NOW()` | `DateTimeOffset` | Insert timestamp |

---

### 10. carrier_quotes

Quote comparison history for decision audit.

| Column | Type | Nullable | Default | .NET Type | Description |
|--------|------|----------|---------|-----------|-------------|
| `id` | UUID | NO | `gen_random_uuid()` | `Guid` | Primary key |
| `order_id` | UUID | YES | — | `Guid?` | FK → orders(id) SET NULL |
| `carrier_code` | VARCHAR(10) | NO | — | `string` | FK → carriers(code) RESTRICT |
| `price_amount` | DECIMAL(10,2) | NO | — | `decimal` | Money.Amount |
| `price_currency` | VARCHAR(5) | NO | `'CNY'` | `string` | CNY or USD |
| `estimated_days` | INTEGER | NO | — | `int` | CHECK: > 0 |
| `service_level` | VARCHAR(20) | NO | — | `string` | Express, Standard, Economy |
| `selected` | BOOLEAN | NO | `FALSE` | `bool` | Was this quote chosen? |
| `quoted_at` | TIMESTAMPTZ | NO | `NOW()` | `DateTimeOffset` | Quote timestamp |

---

## Index Strategy

| Table | Index | Columns | Type | Purpose |
|-------|-------|---------|------|---------|
| users | `idx_users_username` | username | UNIQUE | Auth lookup |
| users | `idx_users_email` | email | UNIQUE | Auth lookup |
| users | `idx_users_role` | role | B-Tree | Role-based queries |
| orders | `idx_orders_order_number` | order_number | UNIQUE | Business key lookup |
| orders | `idx_orders_status` | status | B-Tree | List by status |
| orders | `idx_orders_carrier_code` | carrier_code | B-Tree | Carrier queries |
| orders | `idx_orders_user_id` | user_id | B-Tree | User's orders |
| orders | `idx_orders_created_at` | created_at DESC | B-Tree | Date range queries |
| orders | `idx_orders_service_level` | service_level | B-Tree | Service filter |
| order_items | `idx_order_items_order_id` | order_id | B-Tree | Load with order |
| order_events | `idx_order_events_order_id` | order_id, occurred_at | Composite | Timeline query |
| bookings | `idx_bookings_order_id` | order_id | B-Tree | Order's bookings |
| bookings | `idx_bookings_carrier_code` | carrier_code | B-Tree | Carrier bookings |
| bookings | `idx_bookings_tracking_number` | tracking_number | B-Tree | Track lookup |
| tracking_events | `idx_tracking_events_tracking_number` | tracking_number, occurred_at | Composite | Event stream |
| audit_logs | `idx_audit_entity` | entity_type, entity_id | Composite | Entity timeline |
| audit_logs | `idx_audit_correlation` | correlation_id | B-Tree | Request tracing |
| audit_logs | `idx_audit_timestamp` | timestamp DESC | B-Tree | Recent audit |
| audit_logs | `idx_audit_actor_user_id` | actor_user_id | B-Tree | Actor queries |
| carrier_quotes | `idx_carrier_quotes_order_id` | order_id | B-Tree | Order quotes |
| carrier_quotes | `idx_carrier_quotes_carrier_code` | carrier_code | B-Tree | Carrier analytics |

---

## Value Object Mapping

DDD value objects are flattened into table columns:

| Value Object | Domain Type | DB Columns | Example |
|-------------|------------|------------|---------|
| **ContactInfo** | `ContactInfo` | `customer_name`, `customer_phone`, `customer_email` | 张三, 13812345678 |
| **Address (Origin)** | `Address` | `origin_street`, `origin_city`, `origin_province`, `origin_postal_code`, `origin_country` | 浦东新区..., 上海, Shanghai, 200120, CN |
| **Address (Dest)** | `Address` | `dest_street`, `dest_city`, `dest_province`, `dest_postal_code`, `dest_country` | 天河区..., 广州, Guangdong, 510623, CN |
| **Weight** | `Weight` | `weight_value`, `weight_unit` | 2.500, Kg |
| **Dimension** | `Dimension?` | `dim_length_cm`, `dim_width_cm`, `dim_height_cm` | 35.00, 25.00, 3.00 (or all NULL) |
| **GeoCoordinate** | `GeoCoordinate?` | `location_lat`, `location_lng` | 31.230400, 121.473700 (or both NULL) |
| **Money** | `Money` | `price_amount`, `price_currency` | 35.50, CNY |

---

## Sample Queries

### List all orders with their item counts
```sql
SELECT o.order_number, o.customer_name, o.status, o.service_level,
       COUNT(i.id) AS item_count,
       o.created_at
FROM orders o
LEFT JOIN order_items i ON o.id = i.order_id
GROUP BY o.id
ORDER BY o.created_at DESC;
```

### Get order with all items
```sql
SELECT o.order_number, o.customer_name, o.status,
       i.description, i.quantity, i.weight_value, i.weight_unit,
       i.dim_length_cm, i.dim_width_cm, i.dim_height_cm
FROM orders o
JOIN order_items i ON o.id = i.order_id
WHERE o.id = 'b0000000-0000-0000-0000-000000000001';
```

### Order state transition timeline
```sql
SELECT oe.previous_status, oe.new_status, oe.action, oe.occurred_at
FROM order_events oe
WHERE oe.order_id = 'b0000000-0000-0000-0000-000000000001'
ORDER BY oe.occurred_at;
```

### Full audit trail for an order
```sql
SELECT al.action, al.category, al.actor_name,
       al.correlation_id, al.timestamp,
       al.description, al.payload
FROM audit_logs al
WHERE al.entity_type = 'Order'
  AND al.entity_id = 'b0000000-0000-0000-0000-000000000001'
ORDER BY al.timestamp;
```

### End-to-end request trace by correlation ID
```sql
SELECT al.entity_type, al.entity_id, al.action,
       al.actor_name, al.timestamp, al.description
FROM audit_logs al
WHERE al.correlation_id = 'my-trace-001'
ORDER BY al.timestamp;
```

### Carrier quote comparison for an order
```sql
SELECT cq.carrier_code, cq.price_amount, cq.price_currency,
       cq.estimated_days, cq.service_level, cq.selected
FROM carrier_quotes cq
WHERE cq.order_id = 'b0000000-0000-0000-0000-000000000001'
ORDER BY cq.price_amount;
```

### Tracking event stream for a shipment
```sql
SELECT te.event_type, te.new_status,
       te.location_lat, te.location_lng,
       te.description, te.occurred_at
FROM tracking_events te
WHERE te.tracking_number = 'SF0000000001'
ORDER BY te.occurred_at;
```

### Orders by user with carrier info
```sql
SELECT o.order_number, o.customer_name, o.status,
       o.carrier_code, c.name AS carrier_name,
       o.tracking_number,
       u.display_name AS created_by
FROM orders o
LEFT JOIN carriers c ON o.carrier_code = c.code
LEFT JOIN users u ON o.user_id = u.id
ORDER BY o.created_at DESC;
```

### Database health check (table row counts)
```sql
SELECT 'users' AS table_name, COUNT(*) AS row_count FROM users
UNION ALL SELECT 'carriers', COUNT(*) FROM carriers
UNION ALL SELECT 'orders', COUNT(*) FROM orders
UNION ALL SELECT 'order_items', COUNT(*) FROM order_items
UNION ALL SELECT 'order_events', COUNT(*) FROM order_events
UNION ALL SELECT 'bookings', COUNT(*) FROM bookings
UNION ALL SELECT 'tracking_events', COUNT(*) FROM tracking_events
UNION ALL SELECT 'tracking_snapshots', COUNT(*) FROM tracking_snapshots
UNION ALL SELECT 'audit_logs', COUNT(*) FROM audit_logs
UNION ALL SELECT 'carrier_quotes', COUNT(*) FROM carrier_quotes
ORDER BY table_name;
```
