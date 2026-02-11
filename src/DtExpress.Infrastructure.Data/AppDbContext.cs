using DtExpress.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DtExpress.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for DT-Express TMS.
/// Maps to a PostgreSQL 15+ database with snake_case naming.
/// All Fluent API configuration mirrors database/schema.sql exactly.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DbSets (10 tables) ──────────────────────────────────────
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<CarrierEntity> Carriers => Set<CarrierEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();
    public DbSet<OrderEventEntity> OrderEvents => Set<OrderEventEntity>();
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
    public DbSet<TrackingEventEntity> TrackingEvents => Set<TrackingEventEntity>();
    public DbSet<TrackingSnapshotEntity> TrackingSnapshots => Set<TrackingSnapshotEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<CarrierQuoteEntity> CarrierQuotes => Set<CarrierQuoteEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureCarriers(modelBuilder);
        ConfigureOrders(modelBuilder);
        ConfigureOrderItems(modelBuilder);
        ConfigureOrderEvents(modelBuilder);
        ConfigureBookings(modelBuilder);
        ConfigureTrackingEvents(modelBuilder);
        ConfigureTrackingSnapshots(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
        ConfigureCarrierQuotes(modelBuilder);
    }

    // ═════════════════════════════════════════════════════════════
    // 1. USERS
    // ═════════════════════════════════════════════════════════════
    private static void ConfigureUsers(ModelBuilder mb)
    {
        mb.Entity<UserEntity>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);

            e.Property(u => u.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            e.Property(u => u.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
            e.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(200).IsRequired();
            e.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            e.Property(u => u.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
            e.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            e.HasIndex(u => u.Username).HasDatabaseName("idx_users_username").IsUnique();
            e.HasIndex(u => u.Email).HasDatabaseName("idx_users_email").IsUnique();
            e.HasIndex(u => u.Role).HasDatabaseName("idx_users_role");
        });
    }

    // ═════════════════════════════════════════════════════════════
    // 2. CARRIERS
    // ═════════════════════════════════════════════════════════════
    private static void ConfigureCarriers(ModelBuilder mb)
    {
        mb.Entity<CarrierEntity>(e =>
        {
            e.ToTable("carriers");
            e.HasKey(c => c.Code);

            e.Property(c => c.Code).HasColumnName("code").HasMaxLength(10);
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        });
    }

    // ═════════════════════════════════════════════════════════════
    // 3. ORDERS
    // ═════════════════════════════════════════════════════════════
    private static void ConfigureOrders(ModelBuilder mb)
    {
        mb.Entity<OrderEntity>(e =>
        {
            e.ToTable("orders");
            e.HasKey(o => o.Id);

            e.Property(o => o.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            e.Property(o => o.OrderNumber).HasColumnName("order_number").HasMaxLength(50).IsRequired();

            // ContactInfo (flattened)
            e.Property(o => o.CustomerName).HasColumnName("customer_name").HasMaxLength(200).IsRequired();
            e.Property(o => o.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(20).IsRequired();
            e.Property(o => o.CustomerEmail).HasColumnName("customer_email").HasMaxLength(200);

            // Address: Origin (flattened)
            e.Property(o => o.OriginStreet).HasColumnName("origin_street").HasMaxLength(300).IsRequired();
            e.Property(o => o.OriginCity).HasColumnName("origin_city").HasMaxLength(100).IsRequired();
            e.Property(o => o.OriginProvince).HasColumnName("origin_province").HasMaxLength(50).IsRequired();
            e.Property(o => o.OriginPostalCode).HasColumnName("origin_postal_code").HasMaxLength(10).IsRequired();
            e.Property(o => o.OriginCountry).HasColumnName("origin_country").HasMaxLength(5).HasDefaultValue("CN");

            // Address: Destination (flattened)
            e.Property(o => o.DestStreet).HasColumnName("dest_street").HasMaxLength(300).IsRequired();
            e.Property(o => o.DestCity).HasColumnName("dest_city").HasMaxLength(100).IsRequired();
            e.Property(o => o.DestProvince).HasColumnName("dest_province").HasMaxLength(50).IsRequired();
            e.Property(o => o.DestPostalCode).HasColumnName("dest_postal_code").HasMaxLength(10).IsRequired();
            e.Property(o => o.DestCountry).HasColumnName("dest_country").HasMaxLength(5).HasDefaultValue("CN");

            // Enums as string
            e.Property(o => o.ServiceLevel).HasColumnName("service_level").HasMaxLength(20).IsRequired();
            e.Property(o => o.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Created");

            // Carrier assignment
            e.Property(o => o.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(100);
            e.Property(o => o.CarrierCode).HasColumnName("carrier_code").HasMaxLength(10);

            // User context
            e.Property(o => o.UserId).HasColumnName("user_id");

            // Timestamps
            e.Property(o => o.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            // Indexes
            e.HasIndex(o => o.OrderNumber).HasDatabaseName("idx_orders_order_number").IsUnique();
            e.HasIndex(o => o.Status).HasDatabaseName("idx_orders_status");
            e.HasIndex(o => o.CarrierCode).HasDatabaseName("idx_orders_carrier_code");
            e.HasIndex(o => o.UserId).HasDatabaseName("idx_orders_user_id");
            e.HasIndex(o => o.CreatedAt).HasDatabaseName("idx_orders_created_at").IsDescending();
            e.HasIndex(o => o.ServiceLevel).HasDatabaseName("idx_orders_service_level");

            // FK: carrier
            e.HasOne(o => o.Carrier)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CarrierCode)
                .OnDelete(DeleteBehavior.SetNull);

            // FK: user
            e.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    // ═════════════════════════════════════════════════════════════
    // 4. ORDER_ITEMS
    // ═════════════════════════════════════════════════════════════
    private static void ConfigureOrderItems(ModelBuilder mb)
    {
        mb.Entity<OrderItemEntity>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(i => i.Id);

            e.Property(i => i.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            e.Property(i => i.OrderId).HasColumnName("order_id").IsRequired();
            e.Property(i => i.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            e.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();

            // Weight (flattened)
            e.Property(i => i.WeightValue).HasColumnName("weight_value").HasColumnType("decimal(10,3)").IsRequired();
            e.Property(i => i.WeightUnit).HasColumnName("weight_unit").HasMaxLength(5).IsRequired();

            // Dimension (flattened, nullable group)
            e.Property(i => i.DimLengthCm).HasColumnName("dim_length_cm").HasColumnType("decimal(10,2)");
            e.Property(i => i.DimWidthCm).HasColumnName("dim_width_cm").HasColumnType("decimal(10,2)");
            e.Property(i => i.DimHeightCm).HasColumnName("dim_height_cm").HasColumnType("decimal(10,2)");

            // Index
            e.HasIndex(i => i.OrderId).HasDatabaseName("idx_order_items_order_id");

            // FK: order (cascade delete)
            e.HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ═════════════════════════════════════════════════════════════
    // 5. ORDER_EVENTS
    // ═════════════════════════════════════════════════════════════
    private static void ConfigureOrderEvents(ModelBuilder mb)
    {
        mb.Entity<OrderEventEntity>(e =>
        {
            e.ToTable("order_events");
            e.HasKey(ev => ev.Id);

            e.Property(ev => ev.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            e.Property(ev => ev.OrderId).HasColumnName("order_id").IsRequired();
            e.Property(ev => ev.PreviousStatus).HasColumnName("previous_status").HasMaxLength(20).IsRequired();
            e.Property(ev => ev.NewStatus).HasColumnName("new_status").HasMaxLength(20).IsRequired();
            e.Property(ev => ev.Action).HasColumnName("action").HasMaxLength(20).IsRequired();
            e.Property(ev => ev.OccurredAt).HasColumnName("occurred_at").IsRequired();

            // Composite index: timeline per order
            e.HasIndex(ev => new { ev.OrderId, ev.OccurredAt }).HasDatabaseName("idx_order_events_order_id");

            // FK: order (cascade delete)
            e.HasOne(ev => ev.Order)
                .WithMany(o => o.Events)
                .HasForeignKey(ev => ev.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ═════════════════════════════════════════════════════════════
    // 6. BOOKINGS
    // ═════════════════════════════════════════════════════════════
    private static void ConfigureBookings(ModelBuilder mb)
    {
        mb.Entity<BookingEntity>(e =>
        {
            e.ToTable("bookings");
            e.HasKey(b => b.Id);

            e.Property(b => b.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            e.Property(b => b.OrderId).HasColumnName("order_id");
            e.Property(b => b.CarrierCode).HasColumnName("carrier_code").HasMaxLength(10).IsRequired();
            e.Property(b => b.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(100).IsRequired();
            e.Property(b => b.BookedAt).HasColumnName("booked_at").IsRequired();

            // Indexes
            e.HasIndex(b => b.OrderId).HasDatabaseName("idx_bookings_order_id");
            e.HasIndex(b => b.CarrierCode).HasDatabaseName("idx_bookings_carrier_code");
            e.HasIndex(b => b.TrackingNumber).HasDatabaseName("idx_bookings_tracking_number");

            // FK: order (set null)
            e.HasOne(b => b.Order)
                .WithMany(o => o.Bookings)
                .HasForeignKey(b => b.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // FK: carrier (restrict)
            e.HasOne(b => b.Carrier)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CarrierCode)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ═════════════════════════════════════════════════════════════
    // 7. TRACKING_EVENTS
    // ═════════════════════════════════════════════════════════════
    private static void ConfigureTrackingEvents(ModelBuilder mb)
    {
        mb.Entity<TrackingEventEntity>(e =>
        {
            e.ToTable("tracking_events");
            e.HasKey(t => t.Id);

            e.Property(t => t.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            e.Property(t => t.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(100).IsRequired();
            e.Property(t => t.EventType).HasColumnName("event_type").HasMaxLength(30).IsRequired();
            e.Property(t => t.NewStatus).HasColumnName("new_status").HasMaxLength(30);
            e.Property(t => t.LocationLat).HasColumnName("location_lat").HasColumnType("decimal(9,6)");
            e.Property(t => t.LocationLng).HasColumnName("location_lng").HasColumnType("decimal(9,6)");
            e.Property(t => t.Description).HasColumnName("description").HasColumnType("text");
            e.Property(t => t.OccurredAt).HasColumnName("occurred_at").IsRequired();

            // Composite index: event stream per tracking number
            e.HasIndex(t => new { t.TrackingNumber, t.OccurredAt })
                .HasDatabaseName("idx_tracking_events_tracking_number");
        });
    }

    // ═════════════════════════════════════════════════════════════
    // 8. TRACKING_SNAPSHOTS
    // ═════════════════════════════════════════════════════════════
    private static void ConfigureTrackingSnapshots(ModelBuilder mb)
    {
        mb.Entity<TrackingSnapshotEntity>(e =>
        {
            e.ToTable("tracking_snapshots");
            e.HasKey(s => s.TrackingNumber);

            e.Property(s => s.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(100);
            e.Property(s => s.CurrentStatus).HasColumnName("current_status").HasMaxLength(30).IsRequired();
            e.Property(s => s.LastLocationLat).HasColumnName("last_location_lat").HasColumnType("decimal(9,6)");
            e.Property(s => s.LastLocationLng).HasColumnName("last_location_lng").HasColumnType("decimal(9,6)");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });
    }

    // ═════════════════════════════════════════════════════════════
    // 9. AUDIT_LOGS
    // ═════════════════════════════════════════════════════════════
    private static void ConfigureAuditLogs(ModelBuilder mb)
    {
        mb.Entity<AuditLogEntity>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(a => a.Id);

            e.Property(a => a.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            e.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
            e.Property(a => a.EntityId).HasColumnName("entity_id").HasMaxLength(100).IsRequired();
            e.Property(a => a.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
            e.Property(a => a.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
            e.Property(a => a.ActorUserId).HasColumnName("actor_user_id");
            e.Property(a => a.ActorName).HasColumnName("actor_name").HasMaxLength(200).IsRequired();
            e.Property(a => a.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100).IsRequired();
            e.Property(a => a.Timestamp).HasColumnName("timestamp").IsRequired();
            e.Property(a => a.Description).HasColumnName("description").HasColumnType("text");
            e.Property(a => a.Payload).HasColumnName("payload").HasColumnType("jsonb");
            e.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            // Indexes
            e.HasIndex(a => new { a.EntityType, a.EntityId }).HasDatabaseName("idx_audit_entity");
            e.HasIndex(a => a.CorrelationId).HasDatabaseName("idx_audit_correlation");
            e.HasIndex(a => a.Timestamp).HasDatabaseName("idx_audit_timestamp").IsDescending();
            e.HasIndex(a => a.ActorUserId).HasDatabaseName("idx_audit_actor_user_id");

            // FK: actor user
            e.HasOne(a => a.ActorUser)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    // ═════════════════════════════════════════════════════════════
    // 10. CARRIER_QUOTES
    // ═════════════════════════════════════════════════════════════
    private static void ConfigureCarrierQuotes(ModelBuilder mb)
    {
        mb.Entity<CarrierQuoteEntity>(e =>
        {
            e.ToTable("carrier_quotes");
            e.HasKey(q => q.Id);

            e.Property(q => q.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
            e.Property(q => q.OrderId).HasColumnName("order_id");
            e.Property(q => q.CarrierCode).HasColumnName("carrier_code").HasMaxLength(10).IsRequired();
            e.Property(q => q.PriceAmount).HasColumnName("price_amount").HasColumnType("decimal(10,2)").IsRequired();
            e.Property(q => q.PriceCurrency).HasColumnName("price_currency").HasMaxLength(5).IsRequired();
            e.Property(q => q.EstimatedDays).HasColumnName("estimated_days").IsRequired();
            e.Property(q => q.ServiceLevel).HasColumnName("service_level").HasMaxLength(20).IsRequired();
            e.Property(q => q.QuotedAt).HasColumnName("quoted_at").IsRequired();

            // Indexes
            e.HasIndex(q => q.OrderId).HasDatabaseName("idx_carrier_quotes_order_id");
            e.HasIndex(q => q.CarrierCode).HasDatabaseName("idx_carrier_quotes_carrier_code");

            // FK: order
            e.HasOne(q => q.Order)
                .WithMany()
                .HasForeignKey(q => q.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // FK: carrier
            e.HasOne(q => q.Carrier)
                .WithMany(c => c.Quotes)
                .HasForeignKey(q => q.CarrierCode)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
