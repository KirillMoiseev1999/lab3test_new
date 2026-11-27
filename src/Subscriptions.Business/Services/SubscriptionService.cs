using Subscriptions.Business.Abstractions;
using Subscriptions.Business.Contracts;
using Subscriptions.Business.Errors;
using Subscriptions.Business.Models;
using Subscriptions.Business.Validation;
namespace Subscriptions.Business.Services;


public sealed class SubscriptionService(ISubscriptionStore store, IAtomicExecutor atomic)
{
    public Task<SubscriptionResponse> CreateAsync(Guid user, SubscriptionInput input, CancellationToken ct = default)
    {
        var data = Validate(Guid.NewGuid(), user, input);
        return atomic.ExecuteAsync(async () =>
        {
            await store.AddAsync(data, ct);
            return Map(data);
        }, ct);
    }
    public async Task<SubscriptionResponse> GetAsync(Guid user, Guid id, CancellationToken ct = default) =>
        Map(await OwnedAsync(user, id, ct));
    public Task<SubscriptionResponse> UpdateAsync(Guid user, Guid id, SubscriptionInput input, CancellationToken ct = default)
    {
        var data = Validate(id, user, input);
        return atomic.ExecuteAsync(async () =>
        {
            await OwnedAsync(user, id, ct);
            await store.UpdateAsync(data, ct);
            return Map(data);
        }, ct);
    }
    public Task<bool> ArchiveAsync(Guid user, Guid id, CancellationToken ct = default) =>
        atomic.ExecuteAsync(async () =>
        {
            var data = await OwnedAsync(user, id, ct);
            await store.UpdateAsync(data with { Status = "archived" }, ct);
            return true;
        }, ct);
    public async Task<PageResponse<SubscriptionResponse>> ListAsync(Guid user, string? status,
        string? page, string? size, CancellationToken ct = default)
    {
        if (status is not null) InputValidator.Choice(status, "status", "active", "paused", "archived");
        var paging = InputValidator.Paging(page, size);
        var result = await store.ListAsync(user, status, paging.Page, paging.Size, ct);
        return new(result.Items.Select(Map).ToArray(), paging.Page, paging.Size, result.Count);
    }
    public async Task<SubscriptionData> OwnedAsync(Guid user, Guid id, CancellationToken ct) =>
        await store.FindAsync(user, id, ct) ??
            throw new BusinessException(404, "subscription_not_found", "Подписка не найдена.");
    private static SubscriptionData Validate(Guid id, Guid user, SubscriptionInput input) =>
        new(id, user, InputValidator.Required(input.Name, "name", 100),
            InputValidator.Money(input.Price, "price"),
            InputValidator.Choice(input.Frequency, "frequency", "monthly", "yearly"),
            InputValidator.Date(input.NextPaymentDate, "nextPaymentDate"),
            InputValidator.Choice(input.Status, "status", "active", "paused", "archived"));
    private static SubscriptionResponse Map(SubscriptionData data) =>
        new(data.Id, data.Name, InputValidator.Amount(data.Price),
            data.Frequency, InputValidator.DateText(data.NextPaymentDate), data.Status);
}
