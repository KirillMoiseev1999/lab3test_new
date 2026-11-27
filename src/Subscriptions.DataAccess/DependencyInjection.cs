using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Subscriptions.Business.Abstractions;
using Subscriptions.DataAccess.Repositories;

namespace Subscriptions.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SubscriptionsDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IAuthStore, AuthStore>();
        services.AddScoped<ISubscriptionStore, SubscriptionStore>();
        services.AddScoped<IPaymentStore, PaymentStore>();
        services.AddScoped<IAtomicExecutor, AtomicExecutor>();
        services.AddScoped<IReadinessProbe, SqliteReadinessProbe>();
        return services;
    }
}
