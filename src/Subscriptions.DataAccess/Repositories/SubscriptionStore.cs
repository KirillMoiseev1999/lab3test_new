using Microsoft.EntityFrameworkCore;
using Subscriptions.Business.Abstractions;
using Subscriptions.Business.Models;
using Subscriptions.DataAccess.Entities;
namespace Subscriptions.DataAccess.Repositories;


public sealed class SubscriptionStore(SubscriptionsDbContext db) : ISubscriptionStore
{
    public async Task<SubscriptionData?> FindAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var entity = await db.Subscriptions.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.Id == id, ct);
        return entity is null ? null : Map(entity);
    }
    public Task AddAsync(SubscriptionData data, CancellationToken ct)
    {
        db.Subscriptions.Add(ToEntity(data));
        return Task.CompletedTask;
    }
    public async Task UpdateAsync(SubscriptionData data, CancellationToken ct)
    {
        var entity = await db.Subscriptions.SingleAsync(x => x.Id == data.Id && x.UserId == data.UserId, ct);
        db.Entry(entity).CurrentValues.SetValues(ToEntity(data));
    }
    public Task<bool> HasPaymentsAsync(Guid id, CancellationToken ct) =>
        db.Payments.AnyAsync(x => x.SubscriptionId == id, ct);
    public async Task<(IReadOnlyList<SubscriptionData> Items, int Count)> ListAsync(Guid userId,
        string? status, string? category, int page, int size, CancellationToken ct)
    {
        var query = db.Subscriptions.AsNoTracking().Where(x => x.UserId == userId);
        if (status is not null) query = query.Where(x => x.Status == status);
        if (category is not null) query = query.Where(x => x.Category == category);
        var count = await query.CountAsync(ct);
        var offset = (page - 1L) * size;
        if (offset >= count) return (Array.Empty<SubscriptionData>(), count);
        var items = await query.OrderBy(x => x.NextPaymentDate).ThenBy(x => x.Id)
            .Skip((int)offset).Take(size).ToListAsync(ct);
        return (items.Select(Map).ToArray(), count);
    }
    private static SubscriptionData Map(Subscription e) =>
        new(e.Id, e.UserId, e.Name, e.Category, e.PriceMinor / 100m, e.Currency, e.Frequency, e.NextPaymentDate, e.Status);
    private static Subscription ToEntity(SubscriptionData e) =>
        new()
        {
            Id = e.Id,
            UserId = e.UserId,
            Name = e.Name,
            Category = e.Category,
            PriceMinor = checked((long)(e.Price * 100m)),
            Currency = e.Currency,
            Frequency = e.Frequency,
            NextPaymentDate = e.NextPaymentDate,
            Status = e.Status
        };
}
