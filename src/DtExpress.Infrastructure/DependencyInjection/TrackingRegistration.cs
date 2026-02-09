using DtExpress.Application.Ports;
using DtExpress.Application.Tracking;
using DtExpress.Domain.Tracking.Interfaces;
using DtExpress.Infrastructure.Tracking;
using DtExpress.Infrastructure.Tracking.Observers;
using DtExpress.Infrastructure.Tracking.Ports;
using DtExpress.Infrastructure.Tracking.Sources;
using Microsoft.Extensions.DependencyInjection;

namespace DtExpress.Infrastructure.DependencyInjection;

/// <summary>
/// Registers tracking domain services: subject, sources, observers, and port adapter.
/// </summary>
internal static class TrackingRegistration
{
    internal static IServiceCollection AddTrackingServices(this IServiceCollection services)
    {
        // === Subject (singleton — holds all subscriptions) ===
        services.AddSingleton<ITrackingSubject, InMemoryTrackingSubject>();

        // === Sources (mock event generators) ===
        services.AddSingleton<ITrackingSource, ScriptedTrackingSource>();

        // === Observers ===
        services.AddSingleton<ITrackingObserver, ConsoleTrackingObserver>();

        // === Application services ===
        services.AddScoped<TrackingSubscriptionService>();

        // === Cross-domain port ===
        services.AddScoped<ITrackingPort, TrackingPortAdapter>();

        return services;
    }
}
