-- =============================================================================
-- DT-Express TMS — Docker Init Script
-- =============================================================================
-- This file is mounted into PostgreSQL's /docker-entrypoint-initdb.d/
-- PostgreSQL runs all *.sql files in alphabetical order on first startup.
-- =============================================================================

-- Run schema first, then seed data
\i /docker-entrypoint-initdb.d/01-schema.sql
\i /docker-entrypoint-initdb.d/02-seed-data.sql
