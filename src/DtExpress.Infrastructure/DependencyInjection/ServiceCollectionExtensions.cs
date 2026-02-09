using Microsoft.Extensions.DependencyInjection;

namespace DtExpress.Infrastructure.DependencyInjection;

/// <summary>
/// Master entry point for DT-Express DI registration.
/// <para>
/// Call <c>builder.Services.AddDtExpress()</c> in <c>Program.cs</c> to wire all
/// 5 domains (Routing, Carrier, Tracking, Orders, Audit) plus cross-cutting services.
/// </para>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all DT-Express services.
    /// Single entry point — all 5 domains + cross-cutting concerns.
    /// </summary>
    public static IServiceCollection AddDtExpress(this IServiceCollection services)
    {
        services.AddCommonServices();
        services.AddRoutingServices();
        services.AddCarrierServices();
        services.AddTrackingServices();
        services.AddOrderServices();
        services.AddAuditServices();
        return services;
    }
}
