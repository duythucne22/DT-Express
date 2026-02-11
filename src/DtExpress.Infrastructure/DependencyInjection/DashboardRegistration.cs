using DtExpress.Application.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace DtExpress.Infrastructure.DependencyInjection;

/// <summary>
/// Registers dashboard read services.
/// The EF Core implementation is registered in AddDtExpressData() (Infrastructure.Data).
/// </summary>
internal static class DashboardRegistration
{
    internal static IServiceCollection AddDashboardServices(this IServiceCollection services)
    {
        // IDashboardReadService is registered by AddDtExpressData() since it depends on AppDbContext.
        // This method is a placeholder for any future dashboard-specific services
        // (e.g., caching decorators, materialized view refreshers).
        return services;
    }
}
