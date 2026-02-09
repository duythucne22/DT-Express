using DtExpress.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace DtExpress.Infrastructure.Common;

/// <summary>
/// Implements <see cref="ICommandDispatcher"/> using reflection-based generic resolution.
/// Resolves the correct <see cref="ICommandHandler{TCommand, TResult}"/> from the DI container
/// at runtime by building a closed generic type from the command's concrete type.
/// <para>
/// <strong>ADR-006 compliant</strong>: No switch/if-else chains. Handler resolution is fully
/// generic — adding a new command requires only a new handler class + DI registration.
/// </para>
/// </summary>
public sealed class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public async Task<TResult> DispatchAsync<TResult>(
        ICommand<TResult> command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 1. Get the concrete command type (e.g., CreateOrderCommand)
        var commandType = command.GetType();

        // 2. Build closed generic handler type: ICommandHandler<CreateOrderCommand, Guid>
        var handlerType = typeof(ICommandHandler<,>)
            .MakeGenericType(commandType, typeof(TResult));

        // 3. Resolve from DI container — NO switch/if-else (ADR-006)
        var handler = _serviceProvider.GetRequiredService(handlerType);

        // 4. Invoke HandleAsync via reflection
        var method = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.HandleAsync))
            ?? throw new InvalidOperationException(
                $"Handler for '{commandType.Name}' is missing the HandleAsync method.");

        var result = method.Invoke(handler, [command, ct])
            ?? throw new InvalidOperationException(
                $"Handler for '{commandType.Name}' returned null from HandleAsync.");

        return await (Task<TResult>)result;
    }
}
