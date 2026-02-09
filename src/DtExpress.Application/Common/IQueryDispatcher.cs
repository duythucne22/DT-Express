namespace DtExpress.Application.Common;

/// <summary>Dispatches queries to their handlers via DI resolution.</summary>
public interface IQueryDispatcher
{
    Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}
