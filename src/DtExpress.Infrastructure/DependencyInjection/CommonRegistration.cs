using DtExpress.Application.Common;
using DtExpress.Domain.Common;
using DtExpress.Infrastructure.Common;
using Microsoft.Extensions.DependencyInjection;

namespace DtExpress.Infrastructure.DependencyInjection;

/// <summary>
/// Registers cross-cutting infrastructure services shared by all 5 domains.
/// </summary>
internal static class CommonRegistration
{
    internal static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        // === Cross-cutting ===
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();
        services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();

        // === CQRS Dispatchers ===
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // === Domain Event Publisher ===
        services.AddScoped<IDomainEventPublisher, InMemoryDomainEventPublisher>();

        return services;
    }
}
