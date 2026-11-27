using Subscriptions.Business.Models;
namespace Subscriptions.Business.Abstractions;

public interface ISubscriptionStore
{
    Task<SubscriptionData?> FindAsync(Guid userId, Guid id, CancellationToken ct);
    Task AddAsync(SubscriptionData subscription, CancellationToken ct);
    Task UpdateAsync(SubscriptionData subscription, CancellationToken ct);
    Task<(IReadOnlyList<SubscriptionData> Items, int Count)> ListAsync(Guid userId,
        string? status, int page, int size, CancellationToken ct);
}
