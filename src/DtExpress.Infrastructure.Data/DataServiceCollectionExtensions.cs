using DtExpress.Application.Auth.Models;
using DtExpress.Application.Auth.Services;
using DtExpress.Application.Dashboard;
using DtExpress.Application.Reports;
using DtExpress.Domain.Audit.Interfaces;
using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Infrastructure.Audit.Decorators;
using DtExpress.Infrastructure.Data.Auth;
using DtExpress.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DtExpress.Infrastructure.Data;

/// <summary>
/// Registers EF Core database services for DT-Express.
/// Call <c>services.AddDtExpressData(connectionString)</c> in <c>Program.cs</c>
/// to switch from in-memory stores to PostgreSQL persistence.
/// </summary>
public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Registers AppDbContext + EF repositories, replacing in-memory implementations.
    /// <para>
    /// This replaces <c>IOrderRepository</c>, <c>IOrderReadService</c>,
    /// <c>IAuditSink</c>, and <c>IAuditQueryService</c> with EF Core implementations.
    /// The PII masking decorator is preserved — wraps the EF audit store.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    public static IServiceCollection AddDtExpressData(
        this IServiceCollection services,
        string connectionString)
    {
        // === AppDbContext (scoped — one per HTTP request) ===
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // === State factory (maps OrderStatus → IOrderState) ===
        // States are pure-logic objects with no DI dependencies.
        services.AddSingleton<Func<OrderStatus, IOrderState>>(sp =>
        {
            return status => StateFactory.Create(status);
        });

        // === EF Repositories (scoped — matches DbContext lifetime) ===
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IOrderReadService, EfOrderReadService>();

        // === Dashboard Read Service (Phase 9) ===
        services.AddScoped<IDashboardReadService, EfDashboardReadService>();

        // === Reports Read Service (Phase 9 — Batch 2) ===
        services.AddScoped<IReportReadService, EfReportReadService>();

        // === EF Audit Store (with PII masking decorator) ===
        // Decorator Pattern: PiiMaskingAuditDecorator → EfAuditStore → PostgreSQL
        services.AddScoped<EfAuditStore>();
        services.AddScoped<IAuditSink>(sp =>
            new PiiMaskingAuditDecorator(
                sp.GetRequiredService<EfAuditStore>()));
        services.AddScoped<IAuditQueryService>(sp =>
            sp.GetRequiredService<EfAuditStore>());

        // === Auth Services ===
        services.AddSingleton<RefreshTokenStore>();
        services.AddScoped<JwtTokenService>(sp =>
            new JwtTokenService(sp.GetRequiredService<JwtSettings>()));
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
