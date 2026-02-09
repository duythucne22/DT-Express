using DtExpress.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace DtExpress.Infrastructure.Common;

/// <summary>
/// Implements <see cref="IQueryDispatcher"/> using reflection-based generic resolution.
/// Resolves the correct <see cref="IQueryHandler{TQuery, TResult}"/> from the DI container
/// at runtime by building a closed generic type from the query's concrete type.
/// <para>
/// <strong>ADR-006 compliant</strong>: No switch/if-else chains. Handler resolution is fully
/// generic — adding a new query requires only a new handler class + DI registration.
/// </para>
/// </summary>
public sealed class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public async Task<TResult> DispatchAsync<TResult>(
        IQuery<TResult> query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // 1. Get the concrete query type (e.g., GetOrderByIdQuery)
        var queryType = query.GetType();

        // 2. Build closed generic handler type: IQueryHandler<GetOrderByIdQuery, OrderDetail?>
        var handlerType = typeof(IQueryHandler<,>)
            .MakeGenericType(queryType, typeof(TResult));

        // 3. Resolve from DI container — NO switch/if-else (ADR-006)
        var handler = _serviceProvider.GetRequiredService(handlerType);

        // 4. Invoke HandleAsync via reflection
        var method = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.HandleAsync))
            ?? throw new InvalidOperationException(
                $"Handler for '{queryType.Name}' is missing the HandleAsync method.");

        var result = method.Invoke(handler, [query, ct])
            ?? throw new InvalidOperationException(
                $"Handler for '{queryType.Name}' returned null from HandleAsync.");

        return await (Task<TResult>)result;
    }
}
