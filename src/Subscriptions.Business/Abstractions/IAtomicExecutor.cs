using Subscriptions.Business.Models;
namespace Subscriptions.Business.Abstractions;

public interface IAtomicExecutor
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> work, CancellationToken ct);
}
