using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Subscriptions.Api.Infrastructure;
using Subscriptions.Business.Abstractions;
using Subscriptions.DataAccess;

namespace Subscriptions.IntegrationTests;

public sealed class ApiFactory(string databasePath, TestClock clock) : WebApplicationFactory<ProgramMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<SubscriptionsDbContext>>();
            services.RemoveAll<SubscriptionsDbContext>();
            services.AddDbContext<SubscriptionsDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath};Foreign Keys=True;Default Timeout=30;Pooling=False"));
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(clock);
        });
    }
}
