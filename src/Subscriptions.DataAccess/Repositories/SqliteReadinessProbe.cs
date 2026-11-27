using Microsoft.EntityFrameworkCore;
using Subscriptions.Business.Abstractions;

namespace Subscriptions.DataAccess.Repositories;

public sealed class SqliteReadinessProbe(SubscriptionsDbContext db) : IReadinessProbe
{
    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        if (!await db.Database.CanConnectAsync(ct) || (await db.Database.GetPendingMigrationsAsync(ct)).Any())
            return false;
        await db.Users.Take(1).CountAsync(ct);
        await db.Sessions.Take(1).CountAsync(ct);
        await db.Subscriptions.Take(1).CountAsync(ct);
        await db.Payments.Take(1).CountAsync(ct);
        await db.IdempotencyRecords.Take(1).CountAsync(ct);
        return true;
    }
}
