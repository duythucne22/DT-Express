using System.Threading;
using System.Threading.Tasks;

namespace DtExpress.Application.Common;

/// <summary>Handles a specific command type. One handler per command.</summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}
