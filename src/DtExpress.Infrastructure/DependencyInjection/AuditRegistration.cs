using DtExpress.Application.Ports;
using DtExpress.Domain.Audit.Interfaces;
using DtExpress.Infrastructure.Audit;
using DtExpress.Infrastructure.Audit.Decorators;
using DtExpress.Infrastructure.Audit.Interceptors;
using DtExpress.Infrastructure.Orders.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace DtExpress.Infrastructure.DependencyInjection;

/// <summary>
/// Registers audit domain services: sink (with PII masking decorator), query service,
/// interceptor, and cross-domain port adapter.
/// <para>
/// <strong>Decorator Pattern</strong>: <c>PiiMaskingAuditDecorator</c> wraps the real
/// <c>InMemoryAuditSink</c>. All writes pass through masking before reaching the store.
/// Reads go directly to the store (unmasked data is never stored).
/// </para>
/// </summary>
internal static class AuditRegistration
{
    internal static IServiceCollection AddAuditServices(this IServiceCollection services)
    {
        // === Sink (with PII masking decorator wrapping in-memory store) ===
        services.AddSingleton<InMemoryAuditSink>();
        services.AddSingleton<IAuditSink>(sp =>
            new PiiMaskingAuditDecorator(
                sp.GetRequiredService<InMemoryAuditSink>()));

        // === Query service ===
        // Note: InMemoryAuditSink implements both IAuditSink and IAuditQueryService
        services.AddSingleton<IAuditQueryService>(sp =>
            sp.GetRequiredService<InMemoryAuditSink>());

        // === Interceptor ===
        services.AddScoped<IAuditInterceptor, DomainEventAuditInterceptor>();

        // === Cross-domain port ===
        services.AddScoped<IAuditPort, AuditPortAdapter>();

        return services;
    }
}
