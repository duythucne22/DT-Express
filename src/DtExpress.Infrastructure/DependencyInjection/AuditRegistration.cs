using DtExpress.Application.Ports;
using DtExpress.Domain.Audit.Interfaces;
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
        // === Audit Sink and Query Service ===
        // IAuditSink (with PII decorator) and IAuditQueryService are registered by
        // AddDtExpressData() in the Infrastructure.Data project (EF Core PostgreSQL).
        // The in-memory implementations remain available for testing.

        // === Interceptor ===
        services.AddScoped<IAuditInterceptor, DomainEventAuditInterceptor>();

        // === Cross-domain port ===
        services.AddScoped<IAuditPort, AuditPortAdapter>();

        return services;
    }
}
