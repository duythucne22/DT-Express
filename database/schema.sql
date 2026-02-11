-- =============================================================================
-- DT-Express TMS — PostgreSQL Schema (10 Tables)
-- =============================================================================
-- Version:     1.0
-- Database:    PostgreSQL 15+
-- Encoding:    UTF-8 (supports Chinese characters: 中文)
-- Naming:      snake_case (PostgreSQL convention)
-- Enums:       VARCHAR + CHECK (matches C# enum .ToString())
-- GUIDs:       UUID via gen_random_uuid()
-- Timestamps:  TIMESTAMPTZ (matches DateTimeOffset in .NET)
-- JSON:        JSONB (matches Dictionary<string, object?> in .NET)
-- =============================================================================

-- Drop tables in reverse dependency order (idempotent)
DROP TABLE IF EXISTS carrier_quotes    CASCADE;
DROP TABLE IF EXISTS audit_logs        CASCADE;
DROP TABLE IF EXISTS tracking_snapshots CASCADE;
DROP TABLE IF EXISTS tracking_events   CASCADE;
DROP TABLE IF EXISTS bookings          CASCADE;
DROP TABLE IF EXISTS order_events      CASCADE;
DROP TABLE IF EXISTS order_items       CASCADE;
DROP TABLE IF EXISTS orders            CASCADE;
DROP TABLE IF EXISTS carriers          CASCADE;
DROP TABLE IF EXISTS users             CASCADE;

-- =============================================================================
-- 1. USERS — System users (auth-ready, seeded with test accounts)
-- =============================================================================
-- Maps to: Future IActorProvider / JWT auth
-- Role: Single column + CHECK (4 fixed TMS roles, no RBAC join table needed)
-- Password: BCrypt hash placeholder — see seed-data.sql for instructions
-- =============================================================================
CREATE TABLE users (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    username        VARCHAR(100)    UNIQUE NOT NULL,
    email           VARCHAR(200)    UNIQUE NOT NULL,
    password_hash   VARCHAR(200)    NOT NULL,       -- BCrypt hash (60 chars)
    display_name    VARCHAR(200)    NOT NULL,        -- Human-readable for audit trail
    role            VARCHAR(50)     NOT NULL
        CHECK (role IN ('Admin', 'Dispatcher', 'Driver', 'Viewer')),
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- Indexes: auth lookup by username or email
CREATE INDEX idx_users_username ON users (username);
CREATE INDEX idx_users_email    ON users (email);
CREATE INDEX idx_users_role     ON users (role);

-- =============================================================================
-- 2. CARRIERS — Reference data (seed: SF Express, JD Logistics)
-- =============================================================================
-- Maps to: ICarrierAdapter.CarrierCode (string)
-- Write: Seed only (admin can add carriers)
-- Read:  FK target for orders, bookings, carrier_quotes
-- =============================================================================
CREATE TABLE carriers (
    code        VARCHAR(10)     PRIMARY KEY,        -- "SF", "JD"
    name        VARCHAR(100)    NOT NULL,            -- "顺丰速运", "京东物流"
    is_active   BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- 3. ORDERS — Aggregate root (value objects flattened)
-- =============================================================================
-- Maps to: Domain.Orders.Models.Order (aggregate root)
-- VOs:     ContactInfo → customer_*, Address → origin_*/dest_*
-- FKs:     carrier_code → carriers (nullable, set after booking)
--          user_id → users (nullable until auth Phase 8)
-- =============================================================================
CREATE TABLE orders (
    id                  UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number        VARCHAR(50)     UNIQUE NOT NULL,     -- Business key (IIdGenerator)

    -- ContactInfo value object (flattened)
    customer_name       VARCHAR(200)    NOT NULL,            -- ContactInfo.Name
    customer_phone      VARCHAR(20)     NOT NULL,            -- ContactInfo.Phone (1[3-9]XXXXXXXXX)
    customer_email      VARCHAR(200),                        -- ContactInfo.Email? (nullable)

    -- Address value object: Origin (flattened)
    origin_street       VARCHAR(300)    NOT NULL,            -- Address.Street
    origin_city         VARCHAR(100)    NOT NULL,            -- Address.City
    origin_province     VARCHAR(50)     NOT NULL,            -- Address.Province (Chinese province)
    origin_postal_code  VARCHAR(10)     NOT NULL,            -- Address.PostalCode (6-digit CN)
    origin_country      VARCHAR(5)      NOT NULL DEFAULT 'CN', -- Address.Country (ISO)

    -- Address value object: Destination (flattened)
    dest_street         VARCHAR(300)    NOT NULL,
    dest_city           VARCHAR(100)    NOT NULL,
    dest_province       VARCHAR(50)     NOT NULL,
    dest_postal_code    VARCHAR(10)     NOT NULL,
    dest_country        VARCHAR(5)      NOT NULL DEFAULT 'CN',

    -- Enums (stored as string, matching C# enum .ToString())
    service_level       VARCHAR(20)     NOT NULL
        CHECK (service_level IN ('Express', 'Standard', 'Economy')),
    status              VARCHAR(20)     NOT NULL DEFAULT 'Created'
        CHECK (status IN ('Created', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled')),

    -- Carrier assignment (set after booking, nullable before)
    tracking_number     VARCHAR(100),                        -- Order.TrackingNumber?
    carrier_code        VARCHAR(10)     REFERENCES carriers(code) ON DELETE SET NULL,

    -- User context (nullable until Phase 8 auth)
    user_id             UUID            REFERENCES users(id) ON DELETE SET NULL,

    -- Timestamps (matches DateTimeOffset in .NET)
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- Indexes: OLTP query patterns
CREATE UNIQUE INDEX idx_orders_order_number ON orders (order_number);
CREATE INDEX idx_orders_status              ON orders (status);
CREATE INDEX idx_orders_carrier_code        ON orders (carrier_code);
CREATE INDEX idx_orders_user_id             ON orders (user_id);
CREATE INDEX idx_orders_created_at          ON orders (created_at DESC);
CREATE INDEX idx_orders_service_level       ON orders (service_level);

-- =============================================================================
-- 4. ORDER_ITEMS — Owned by Order (cascade delete)
-- =============================================================================
-- Maps to: Domain.Orders.Models.OrderItem
-- VOs:     Weight → weight_*, Dimension? → dim_* (nullable group)
-- =============================================================================
CREATE TABLE order_items (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id        UUID            NOT NULL REFERENCES orders(id) ON DELETE CASCADE,

    description     VARCHAR(500)    NOT NULL,            -- OrderItem.Description
    quantity        INTEGER         NOT NULL CHECK (quantity > 0),  -- OrderItem.Quantity

    -- Weight value object (flattened)
    weight_value    DECIMAL(10,3)   NOT NULL,            -- Weight.Value
    weight_unit     VARCHAR(5)      NOT NULL             -- WeightUnit enum
        CHECK (weight_unit IN ('Kg', 'G', 'Jin', 'Lb')),

    -- Dimension value object (flattened, ALL nullable as a group)
    dim_length_cm   DECIMAL(10,2),                       -- Dimension?.LengthCm
    dim_width_cm    DECIMAL(10,2),                       -- Dimension?.WidthCm
    dim_height_cm   DECIMAL(10,2),                       -- Dimension?.HeightCm

    -- Constraint: dimensions are all-or-nothing
    CONSTRAINT chk_dimensions_all_or_none CHECK (
        (dim_length_cm IS NULL AND dim_width_cm IS NULL AND dim_height_cm IS NULL)
        OR
        (dim_length_cm IS NOT NULL AND dim_width_cm IS NOT NULL AND dim_height_cm IS NOT NULL
         AND dim_length_cm > 0 AND dim_width_cm > 0 AND dim_height_cm > 0)
    )
);

-- Index: load items with order
CREATE INDEX idx_order_items_order_id ON order_items (order_id);

-- =============================================================================
-- 5. ORDER_EVENTS — Domain event log (append-only, never updated)
-- =============================================================================
-- Maps to: Domain.Orders.Models.OrderDomainEvent
-- Purpose: State machine transition history (Event Sourcing Lite)
-- =============================================================================
CREATE TABLE order_events (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id        UUID            NOT NULL REFERENCES orders(id) ON DELETE CASCADE,

    previous_status VARCHAR(20)     NOT NULL
        CHECK (previous_status IN ('Created', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled')),
    new_status      VARCHAR(20)     NOT NULL
        CHECK (new_status IN ('Created', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled')),
    action          VARCHAR(20)     NOT NULL
        CHECK (action IN ('Confirm', 'Ship', 'Deliver', 'Cancel')),

    occurred_at     TIMESTAMPTZ     NOT NULL
);

-- Index: timeline query per order
CREATE INDEX idx_order_events_order_id ON order_events (order_id, occurred_at);

-- =============================================================================
-- 6. BOOKINGS — Carrier booking records
-- =============================================================================
-- Maps to: Domain.Carrier.Models.BookingResult
-- Links:   order (nullable — standalone bookings possible)
--          carrier (required — every booking has a carrier)
-- =============================================================================
CREATE TABLE bookings (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id        UUID            REFERENCES orders(id) ON DELETE SET NULL,
    carrier_code    VARCHAR(10)     NOT NULL REFERENCES carriers(code) ON DELETE RESTRICT,
    tracking_number VARCHAR(100)    NOT NULL UNIQUE,     -- BookingResult.TrackingNumber (UNIQUE for FK target)
    booked_at       TIMESTAMPTZ     NOT NULL             -- BookingResult.BookedAt
);

-- Indexes: lookup patterns
CREATE INDEX idx_bookings_order_id          ON bookings (order_id);
CREATE INDEX idx_bookings_carrier_code      ON bookings (carrier_code);
CREATE INDEX idx_bookings_tracking_number   ON bookings (tracking_number);

-- =============================================================================
-- 7. TRACKING_EVENTS — Append-only event stream
-- =============================================================================
-- Maps to: Domain.Tracking.Models.TrackingEvent
-- VOs:     GeoCoordinate? → location_lat/location_lng (nullable pair)
-- =============================================================================
CREATE TABLE tracking_events (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    tracking_number VARCHAR(100)    NOT NULL REFERENCES bookings(tracking_number) ON DELETE CASCADE,  -- FK to bookings

    event_type      VARCHAR(30)     NOT NULL
        CHECK (event_type IN ('StatusChanged', 'LocationUpdated')),
    new_status      VARCHAR(30)                          -- ShipmentStatus? (nullable for LocationUpdated)
        CHECK (new_status IS NULL OR new_status IN (
            'Created', 'PickedUp', 'InTransit', 'OutForDelivery', 'Delivered', 'Exception'
        )),

    -- GeoCoordinate value object (nullable pair)
    location_lat    DECIMAL(9,6),                        -- GeoCoordinate?.Latitude
    location_lng    DECIMAL(9,6),                        -- GeoCoordinate?.Longitude

    description     TEXT,                                -- TrackingEvent.Description?
    occurred_at     TIMESTAMPTZ     NOT NULL,            -- TrackingEvent.OccurredAt

    -- Constraint: lat/lng are both null or both populated
    CONSTRAINT chk_location_pair CHECK (
        (location_lat IS NULL AND location_lng IS NULL)
        OR
        (location_lat IS NOT NULL AND location_lng IS NOT NULL)
    )
);

-- Index: event stream per tracking number
CREATE INDEX idx_tracking_events_tracking_number ON tracking_events (tracking_number, occurred_at);

-- =============================================================================
-- 8. TRACKING_SNAPSHOTS — Materialized latest state (one per shipment)
-- =============================================================================
-- Maps to: Domain.Tracking.Models.TrackingSnapshot
-- Write:   UPSERT on each tracking event (INSERT ON CONFLICT UPDATE)
-- Read:    Single-row lookup by tracking_number
-- =============================================================================
CREATE TABLE tracking_snapshots (
    tracking_number VARCHAR(100)    PRIMARY KEY REFERENCES bookings(tracking_number) ON DELETE CASCADE,  -- FK to bookings

    current_status  VARCHAR(30)     NOT NULL
        CHECK (current_status IN (
            'Created', 'PickedUp', 'InTransit', 'OutForDelivery', 'Delivered', 'Exception'
        )),

    -- GeoCoordinate value object (nullable pair)
    last_location_lat   DECIMAL(9,6),                    -- GeoCoordinate?.Latitude
    last_location_lng   DECIMAL(9,6),                    -- GeoCoordinate?.Longitude

    updated_at      TIMESTAMPTZ     NOT NULL,

    CONSTRAINT chk_snapshot_location_pair CHECK (
        (last_location_lat IS NULL AND last_location_lng IS NULL)
        OR
        (last_location_lat IS NOT NULL AND last_location_lng IS NOT NULL)
    )
);

-- =============================================================================
-- 9. AUDIT_LOGS — Immutable audit trail (append-only, never updated/deleted)
-- =============================================================================
-- Maps to: Domain.Audit.Models.AuditRecord
-- Actor:   Dual reference pattern:
--          actor_user_id (FK for joins, nullable until auth)
--          actor_name (string snapshot, always populated, survives user deletion)
-- =============================================================================
CREATE TABLE audit_logs (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),

    entity_type     VARCHAR(50)     NOT NULL,            -- "Order", "Booking", "Route"
    entity_id       VARCHAR(100)    NOT NULL,            -- Entity identifier

    action          VARCHAR(50)     NOT NULL
        CHECK (action IN ('Created', 'Updated', 'Deleted', 'StateChanged', 'BusinessAction')),
    category        VARCHAR(50)     NOT NULL
        CHECK (category IN ('DataChange', 'StateTransition', 'ExternalCall', 'BusinessDecision')),

    -- Actor: dual reference (FK + text snapshot)
    actor_user_id   UUID            REFERENCES users(id) ON DELETE SET NULL,
    actor_name      VARCHAR(200)    NOT NULL,            -- Always populated (display without JOIN)

    correlation_id  VARCHAR(100)    NOT NULL,            -- Distributed tracing

    timestamp       TIMESTAMPTZ     NOT NULL,            -- AuditRecord.Timestamp
    description     TEXT,                                -- AuditRecord.Description?
    payload         JSONB,                               -- AuditRecord.Payload (Before/After diffs)

    -- Prevent any modification after insert (application-level, but schema signals intent)
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- Indexes: query patterns for audit
CREATE INDEX idx_audit_entity          ON audit_logs (entity_type, entity_id);
CREATE INDEX idx_audit_correlation     ON audit_logs (correlation_id);
CREATE INDEX idx_audit_timestamp       ON audit_logs (timestamp DESC);
CREATE INDEX idx_audit_actor_user_id   ON audit_logs (actor_user_id);

-- =============================================================================
-- OPTIONAL: Add FK from orders.tracking_number to bookings.tracking_number
-- =============================================================================
-- Uncomment after initial schema creation if strict referential integrity needed:
-- ALTER TABLE orders ADD CONSTRAINT fk_orders_tracking_number
--     FOREIGN KEY (tracking_number) REFERENCES bookings(tracking_number) ON DELETE SET NULL;
--
-- NOTE: This enforces that orders.tracking_number must exist in bookings,
--       preventing orphaned tracking references in orders table.
-- =============================================================================

-- =============================================================================
-- 10. CARRIER_QUOTES — Quote comparison history (decision audit trail)
-- =============================================================================
-- Maps to: Domain.Carrier.Models.CarrierQuote
-- Purpose: Shows quote comparison analytics, decision audit trail
-- =============================================================================
CREATE TABLE carrier_quotes (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id        UUID            REFERENCES orders(id) ON DELETE SET NULL,
    carrier_code    VARCHAR(10)     NOT NULL REFERENCES carriers(code) ON DELETE RESTRICT,

    -- Money value object (flattened)
    price_amount    DECIMAL(10,2)   NOT NULL,            -- Money.Amount
    price_currency  VARCHAR(5)      NOT NULL DEFAULT 'CNY' -- Money.Currency
        CHECK (price_currency IN ('CNY', 'USD')),

    estimated_days  INTEGER         NOT NULL CHECK (estimated_days > 0),
    service_level   VARCHAR(20)     NOT NULL
        CHECK (service_level IN ('Express', 'Standard', 'Economy')),

    selected        BOOLEAN         NOT NULL DEFAULT FALSE,  -- Was this quote chosen?
    quoted_at       TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- Indexes: analytics patterns
CREATE INDEX idx_carrier_quotes_order_id     ON carrier_quotes (order_id);
CREATE INDEX idx_carrier_quotes_carrier_code ON carrier_quotes (carrier_code);

-- =============================================================================
-- VERIFICATION QUERY (run after schema creation)
-- =============================================================================
-- SELECT table_name FROM information_schema.tables
-- WHERE table_schema = 'public' ORDER BY table_name;
--
-- Expected: 10 tables
--   audit_logs, bookings, carrier_quotes, carriers,
--   order_events, order_items, orders,
--   tracking_events, tracking_snapshots, users
-- =============================================================================
