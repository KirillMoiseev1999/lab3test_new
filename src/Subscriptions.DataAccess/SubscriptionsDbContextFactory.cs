using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Subscriptions.DataAccess;

public sealed class SubscriptionsDbContextFactory : IDesignTimeDbContextFactory<SubscriptionsDbContext>
{
    public SubscriptionsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SubscriptionsDbContext>()
            .UseSqlite("Data Source=subscriptions-simple.db;Foreign Keys=True;Default Timeout=30").Options);
}
