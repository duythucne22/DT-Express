using System.Threading;
using System.Threading.Tasks;

namespace DtExpress.Application.Common;

/// <summary>Handles a specific query type. One handler per query.</summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}
