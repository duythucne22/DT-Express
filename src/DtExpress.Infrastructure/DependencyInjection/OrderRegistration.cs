using DtExpress.Application.Common;
using DtExpress.Application.Orders.Commands;
using DtExpress.Application.Orders.Handlers;
using DtExpress.Application.Orders.Queries;
using DtExpress.Domain.Carrier.Models;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;
using DtExpress.Infrastructure.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace DtExpress.Infrastructure.DependencyInjection;

/// <summary>
/// Registers order domain services: repository, read service, command/query handlers.
/// <para>
/// <strong>State Pattern</strong>: States are NOT registered in DI. They are pure-logic
/// objects created directly: <c>new CreatedState()</c>, etc. No external dependencies.
/// </para>
/// </summary>
internal static class OrderRegistration
{
    internal static IServiceCollection AddOrderServices(this IServiceCollection services)
    {
        // === State implementations (transient — new instance per order action) ===
        // States are NOT registered in DI. They are created by the Order aggregate or
        // by the repository when loading. States are pure logic, no dependencies.

        // === Repositories (singleton — in-memory store) ===
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        services.AddSingleton<IOrderReadService, InMemoryOrderReadService>();

        // === CQRS Command Handlers ===
        services.AddScoped<ICommandHandler<CreateOrderCommand, Guid>, CreateOrderHandler>();
        services.AddScoped<ICommandHandler<ConfirmOrderCommand, bool>, ConfirmOrderHandler>();
        services.AddScoped<ICommandHandler<ShipOrderCommand, BookingResult>, ShipOrderHandler>();
        services.AddScoped<ICommandHandler<DeliverOrderCommand, bool>, DeliverOrderHandler>();
        services.AddScoped<ICommandHandler<CancelOrderCommand, bool>, CancelOrderHandler>();

        // === CQRS Query Handlers ===
        services.AddScoped<IQueryHandler<GetOrderByIdQuery, OrderDetail?>, GetOrderByIdHandler>();
        services.AddScoped<IQueryHandler<ListOrdersQuery, IReadOnlyList<OrderSummary>>, ListOrdersHandler>();

        return services;
    }
}
