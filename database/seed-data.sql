-- =============================================================================
-- DT-Express TMS — Seed Data
-- =============================================================================
-- Run AFTER schema.sql
-- =============================================================================

-- =============================================================================
-- 1. USERS — Test accounts (4 TMS roles)
-- =============================================================================
-- Password hashing: BCrypt (work factor 12)
--
-- ⚠️  DO NOT use these hashes directly. Generate your own:
--
--   Algorithm:  BCrypt with work factor 12
--   Library:    BCrypt.Net-Next (NuGet: BCrypt.Net-Next)
--   C# code:
--
--     using BCrypt.Net;
--     string hash = BCrypt.Net.BCrypt.HashPassword("YourPassword123!", 12);
--     Console.WriteLine(hash);
--
--   Or from terminal:
--     dotnet add package BCrypt.Net-Next
--     dotnet script -e "Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(\"admin123\", 12));"
--     passwd123
-- Placeholder hashes below are for: "Password123!"
-- REPLACE with actual BCrypt hashes before production use.
-- =============================================================================

INSERT INTO users (id, username, email, password_hash, display_name, role) VALUES
(
    'a0000000-0000-0000-0000-000000000001',
    'admin',
    'admin@dtexpress.com',
    '$2a$12$qk3gR5Rxk5HFMJi7aR6X4uMPNgohyW90OgN0eBBtTBhklr6ZT.r4i',            -- Replace with BCrypt hash of your chosen password
    '系统管理员',                              -- System Administrator
    'Admin'
),
(
    'a0000000-0000-0000-0000-000000000002',
    'dispatcher',
    'dispatcher@dtexpress.com',
    '$2a$12$Xb5JdRkYxc/LPdYAeNlU9uJalPXg9OucBM7K3aI0hzD7yo1kmb4YO',         -- Replace with BCrypt hash
    '调度员小李',                              -- Dispatcher Li
    'Dispatcher'
),
(
    'a0000000-0000-0000-0000-000000000003',
    'driver',
    'driver@dtexpress.com',
    '$2a$12$Xb5JdRkYxc/LPdYAeNlU9uJalPXg9OucBM7K3aI0hzD7yo1kmb4YO',           -- Replace with BCrypt hash
    '司机王师傅',                              -- Driver Wang
    'Driver'
),
(
    'a0000000-0000-0000-0000-000000000004',
    'viewer',
    'viewer@dtexpress.com',
    '$2a$12$Xb5JdRkYxc/LPdYAeNlU9uJalPXg9OucBM7K3aI0hzD7yo1kmb4YO',           -- Replace with BCrypt hash
    '客服张小姐',                              -- Customer Support Zhang
    'Viewer'
);

-- =============================================================================
-- 2. CARRIERS — SF Express and JD Logistics
-- =============================================================================
-- Matches ICarrierAdapter implementations: SfExpressAdapter, JdLogisticsAdapter
-- =============================================================================

INSERT INTO carriers (code, name, is_active) VALUES
('SF', '顺丰速运', TRUE),   -- SF Express
('JD', '京东物流', TRUE);    -- JD Logistics

-- =============================================================================
-- 3. SAMPLE ORDER — For testing (demonstrates full data structure)
-- =============================================================================
-- This order shows:
--   - Chinese addresses with proper province/postal code
--   - ContactInfo value object flattened
--   - Express service level
--   - Status: Created (initial state)
--   - No carrier/tracking yet (pre-booking)
--   - user_id NULL (pre-auth)
-- =============================================================================

INSERT INTO orders (
    id, order_number,
    customer_name, customer_phone, customer_email,
    origin_street, origin_city, origin_province, origin_postal_code, origin_country,
    dest_street, dest_city, dest_province, dest_postal_code, dest_country,
    service_level, status,
    tracking_number, carrier_code, user_id
) VALUES (
    'b0000000-0000-0000-0000-000000000001',
    'DT-20260210-0001',
    '张三',                    -- Customer name
    '13812345678',             -- Chinese mobile (valid 1[3-9]XXXXXXXXX)
    'zhangsan@example.com',
    '浦东新区陆家嘴环路1000号',   -- Origin street
    '上海',                    -- Origin city
    'Shanghai',                -- Origin province (English for validation)
    '200120',                  -- 6-digit CN postal code
    'CN',
    '天河区珠江新城花城大道18号',   -- Dest street
    '广州',                    -- Dest city
    'Guangdong',               -- Dest province
    '510623',                  -- 6-digit CN postal code
    'CN',
    'Express',                 -- ServiceLevel enum
    'Created',                 -- OrderStatus enum (initial state)
    NULL,                      -- No tracking number yet
    NULL,                      -- No carrier assigned yet
    NULL                       -- No user yet (pre-auth)
);

-- Order items for the sample order
INSERT INTO order_items (
    id, order_id,
    description, quantity,
    weight_value, weight_unit,
    dim_length_cm, dim_width_cm, dim_height_cm
) VALUES
(
    'c0000000-0000-0000-0000-000000000001',
    'b0000000-0000-0000-0000-000000000001',
    '电子产品 - 笔记本电脑',     -- Electronics - Laptop
    1,
    2.500, 'Kg',               -- 2.5 Kg
    35.00, 25.00, 3.00         -- Laptop dimensions
),
(
    'c0000000-0000-0000-0000-000000000002',
    'b0000000-0000-0000-0000-000000000001',
    '配件 - 充电器和鼠标',       -- Accessories - Charger and Mouse
    2,
    0.300, 'Kg',               -- 300g
    NULL, NULL, NULL           -- No dimensions (small items)
);

-- =============================================================================
-- 4. SAMPLE AUDIT LOG — Demonstrates audit trail for order creation
-- =============================================================================

INSERT INTO audit_logs (
    id, entity_type, entity_id,
    action, category,
    actor_user_id, actor_name,
    correlation_id, timestamp,
    description, payload
) VALUES (
    'd0000000-0000-0000-0000-000000000001',
    'Order',
    'b0000000-0000-0000-0000-000000000001',
    'Created',
    'DataChange',
    NULL,                      -- No user yet (system action)
    'system',                  -- Matches current hard-coded actor
    'seed-correlation-001',
    NOW(),
    'Order DT-20260210-0001 created (seed data)',
    '{"orderNumber": "DT-20260210-0001", "customerName": "张三", "serviceLevel": "Express"}'::jsonb
);

-- =============================================================================
-- VERIFICATION QUERIES
-- =============================================================================
-- SELECT COUNT(*) AS user_count FROM users;           -- Expected: 4
-- SELECT COUNT(*) AS carrier_count FROM carriers;     -- Expected: 2
-- SELECT COUNT(*) AS order_count FROM orders;         -- Expected: 1
-- SELECT COUNT(*) AS item_count FROM order_items;     -- Expected: 2
-- SELECT COUNT(*) AS audit_count FROM audit_logs;     -- Expected: 1
-- =============================================================================
