using Microsoft.EntityFrameworkCore;
using Subscriptions.Business.Abstractions;
using Subscriptions.Business.Models;
using Subscriptions.DataAccess.Entities;
namespace Subscriptions.DataAccess.Repositories;


public sealed class PaymentStore(SubscriptionsDbContext db) : IPaymentStore
{
    public async Task<IdempotencyData?> FindKeyAsync(Guid userId, string key, CancellationToken ct)
    {
        var record = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.Key == key, ct);
        if (record is null) return null;
        var payment = await db.Payments.AsNoTracking().SingleAsync(x => x.Id == record.PaymentId, ct);
        return new(record.Id, record.UserId, record.Key, Map(payment), new DateTimeOffset(record.CreatedAtUtcTicks, TimeSpan.Zero));
    }
    public async Task SaveAsync(PaymentData payment, IdempotencyData record, CancellationToken ct)
    {
        db.Payments.Add(new Payment
        {
            Id = payment.Id,
            SubscriptionId = payment.SubscriptionId,
            AmountMinor = checked((long)(payment.Amount * 100m)),
            PaidOn = payment.PaidOn,
            CreatedAtUtcTicks = payment.CreatedAtUtc.UtcTicks
        });
        var entity = await db.IdempotencyRecords.SingleOrDefaultAsync(x => x.Id == record.Id, ct);
        if (entity is null)
        {
            entity = new IdempotencyRecord { Id = record.Id, UserId = record.UserId, Key = record.Key };
            db.IdempotencyRecords.Add(entity);
        }
        entity.PaymentId = payment.Id;
        entity.CreatedAtUtcTicks = record.CreatedAtUtc.UtcTicks;
    }
    public async Task<(IReadOnlyList<PaymentData> Items, int Count)> ListAsync(Guid userId, Guid? subscriptionId,
        DateOnly start, DateOnly end, int page, int size, CancellationToken ct)
    {
        var query = from p in db.Payments.AsNoTracking()
                    join s in db.Subscriptions on p.SubscriptionId equals s.Id
                    where s.UserId == userId && p.PaidOn >= start && p.PaidOn < end
                    select p;
        if (subscriptionId is not null) query = query.Where(x => x.SubscriptionId == subscriptionId);
        var count = await query.CountAsync(ct);
        var offset = (page - 1L) * size;
        if (offset >= count) return (Array.Empty<PaymentData>(), count);
        var items = await query.OrderByDescending(x => x.PaidOn).ThenBy(x => x.Id)
            .Skip((int)offset).Take(size).ToListAsync(ct);
        return (items.Select(Map).ToArray(), count);
    }
    public async Task<IReadOnlyList<ExpenseData>> ExpensesAsync(Guid userId, DateOnly start, DateOnly end, CancellationToken ct) =>
        await (from payment in db.Payments.AsNoTracking()
               join subscription in db.Subscriptions on payment.SubscriptionId equals subscription.Id
               where subscription.UserId == userId && payment.PaidOn >= start && payment.PaidOn < end
               select new ExpenseData(subscription.Id, subscription.Name, payment.AmountMinor / 100m)).ToListAsync(ct);
    private static PaymentData Map(Payment e) =>
        new(e.Id, e.SubscriptionId, e.AmountMinor / 100m, e.PaidOn, new DateTimeOffset(e.CreatedAtUtcTicks, TimeSpan.Zero));
}
