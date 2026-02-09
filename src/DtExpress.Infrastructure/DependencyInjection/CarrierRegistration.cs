using DtExpress.Application.Carrier;
using DtExpress.Application.Ports;
using DtExpress.Domain.Carrier.Interfaces;
using DtExpress.Infrastructure.Carrier;
using DtExpress.Infrastructure.Carrier.Adapters;
using DtExpress.Infrastructure.Carrier.Ports;
using DtExpress.Infrastructure.Carrier.Selectors;
using Microsoft.Extensions.DependencyInjection;

namespace DtExpress.Infrastructure.DependencyInjection;

/// <summary>
/// Registers carrier domain services: adapters, factory, selector, and port adapter.
/// <para>
/// <strong>OCP</strong>: Adding a new carrier = new adapter class + ONE line here.
/// Zero changes to <c>CarrierAdapterFactory</c>, <c>CarrierQuotingService</c>, or controllers.
/// </para>
/// </summary>
internal static class CarrierRegistration
{
    internal static IServiceCollection AddCarrierServices(this IServiceCollection services)
    {
        // === Adapters (register each as ICarrierAdapter) ===
        // OCP: Add new carrier = add ONE line here + new adapter class
        services.AddSingleton<ICarrierAdapter, SfExpressAdapter>();
        services.AddSingleton<ICarrierAdapter, JdLogisticsAdapter>();

        // === Factory (builds registry from IEnumerable<ICarrierAdapter>) ===
        services.AddSingleton<ICarrierAdapterFactory, CarrierAdapterFactory>();

        // === Selection strategy ===
        services.AddSingleton<ICarrierSelector, CheapestCarrierSelector>();

        // === Application services ===
        services.AddScoped<CarrierQuotingService>();
        services.AddScoped<CarrierBookingService>();

        // === Cross-domain port ===
        services.AddScoped<ICarrierPort, CarrierPortAdapter>();

        return services;
    }
}
