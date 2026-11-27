using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Subscriptions.Business.Abstractions;
namespace Subscriptions.DataAccess.Repositories;


public sealed class AtomicExecutor(SubscriptionsDbContext db) : IAtomicExecutor
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> work, CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        try
        {
            // An immediate transaction serializes writers before reading an idempotency key.
            // A competing request waits and then reads the committed result.
            for (var attempt = 0; ; attempt++)
            {
                using var transaction = connection.BeginTransaction(deferred: false);
                await using var enlisted = await db.Database.UseTransactionAsync(transaction, ct);
                try
                {
                    var result = await work();
                    await db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    return result;
                }
                catch (DbUpdateException ex) when (attempt == 0 && ex.InnerException is SqliteException { SqliteErrorCode: 19 })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    db.ChangeTracker.Clear();
                    // Re-read after a unique-key conflict and apply the business replay policy.
                }
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}

