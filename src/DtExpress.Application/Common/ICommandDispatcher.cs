namespace DtExpress.Application.Common;

/// <summary>
/// Dispatches commands to their handlers via DI resolution.
/// No switch/if-else — uses IServiceProvider.GetRequiredService with generic type.
/// </summary>
public interface ICommandDispatcher
{
    Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default);
}
