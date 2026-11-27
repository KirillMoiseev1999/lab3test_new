using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Subscriptions.Business.Contracts;
using Subscriptions.DataAccess;
using Subscriptions.DataAccess.Entities;
using Subscriptions.DataAccess.Repositories;

namespace Subscriptions.IntegrationTests;

[NonParallelizable]
public class ApiTests
{
    private string directory = null!;
    private string database = null!;
    private ApiFactory factory = null!;
    private HttpClient client = null!;
    private TestClock clock = null!;
    private const string Password = "Demo-password-2026";
    private readonly SubscriptionInput input = new("Music", "Media", "100.00", "RUB", "monthly", "2026-12-01", "active");

    [SetUp]
    public void Setup()
    {
        directory = Path.Combine(Path.GetTempPath(), "subscriptions-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        database = Path.Combine(directory, "test.db");
        clock = new();
        factory = new(database, clock);
        client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    }
    [TearDown]
    public void Cleanup()
    {
        client.Dispose(); factory.Dispose();
        Directory.Delete(directory, true);
    }
    private async Task<LoginResponse> Login(string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new AuthInput(email, Password));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }
    [Test]
    public async Task ReadinessReturns503WhenDatabaseSchemaIsMissing()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>();
            await db.Database.EnsureDeletedAsync();
        }
        var response = await Send(HttpMethod.Get, "/health/ready");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }
    private async Task<string> User(string email = "user@example.com")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new AuthInput(email, Password));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
        return (await Login(email)).AccessToken;
    }
    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? token = null, object? body = null, string? key = null)
    {
        using var request = new HttpRequestMessage(method, "/api/v1" + path);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (key is not null) request.Headers.Add("Idempotency-Key", key);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }
    private async Task<SubscriptionResponse> Subscription(string token, SubscriptionInput? value = null)
    {
        var response = await Send(HttpMethod.Post, "/subscriptions", token, value ?? input);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<SubscriptionResponse>())!;
    }
    private async Task<HttpResponseMessage> Pay(string token, Guid id, string key, string amount = "100.00", string date = "2026-11-27") =>
        await Send(HttpMethod.Post, $"/subscriptions/{id}/payments", token, new PaymentInput(amount, "RUB", date), key);

    [Test]
    public async Task ReadyAndOpenApiDescribeOnlyRequiredEndpoints()
    {
        Assert.That((await Send(HttpMethod.Get, "/health/ready")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = json.RootElement.GetProperty("paths");
        Assert.That(paths.EnumerateObject().Sum(p => p.Value.EnumerateObject().Count()), Is.EqualTo(12));
        Assert.That(paths.TryGetProperty("/api/v1/calendar", out _), Is.False);
        Assert.That(json.RootElement.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _), Is.True);
    }
    [Test]
    public async Task RegistrationNormalizesEmailAndConcurrentDuplicatesConflict()
    {
        var first = await client.PostAsJsonAsync("/api/v1/auth/register", new AuthInput(" User@Example.com ", Password));
        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/v1/auth/register", new AuthInput("user@example.com", Password))));
        Assert.That(responses.All(r => r.StatusCode == HttpStatusCode.Conflict), Is.True);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>();
        Assert.That(await db.Users.CountAsync(), Is.EqualTo(1));
        Assert.That((await db.Users.SingleAsync()).PasswordHash, Is.Not.EqualTo(Password));
    }
    [Test]
    public async Task LoginLimitExpiresAtFifteenMinuteBoundary()
    {
        await User();
        for (var i = 0; i < 5; i++)
        {
            var bad = await client.PostAsJsonAsync("/api/v1/auth/login", new AuthInput("user@example.com", "wrong-password-2026"));
            Assert.That(bad.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
        var blocked = await client.PostAsJsonAsync("/api/v1/auth/login", new AuthInput("user@example.com", Password));
        Assert.That(blocked.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        clock.UtcNow = clock.UtcNow.AddMinutes(15);
        await Login("user@example.com");
    }
    [Test]
    public async Task SessionsExpireAtBoundaryAndLogoutRevokesOnlyCurrentToken()
    {
        var token = await User();
        var other = (await Login("user@example.com")).AccessToken;
        Assert.That((await Send(HttpMethod.Post, "/auth/logout", token)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await Send(HttpMethod.Get, "/subscriptions", token)).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await Send(HttpMethod.Get, "/subscriptions", other)).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        clock.UtcNow = clock.UtcNow.AddMinutes(30);
        Assert.That((await Send(HttpMethod.Get, "/subscriptions", other)).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await Send(HttpMethod.Get, "/subscriptions")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
    [Test]
    public async Task CrudPaginationAndOwnerChecksWorkTogether()
    {
        var owner = await User();
        var foreign = await User("other@example.com");
        var first = await Subscription(owner);
        await Subscription(owner, input with { Name = "Other", Category = "Other", Status = "paused" });
        var filtered = await Send(HttpMethod.Get, "/subscriptions?category=Media&status=active&pageSize=1", owner);
        var list = (await filtered.Content.ReadFromJsonAsync<PageResponse<SubscriptionResponse>>())!;
        Assert.That(list.TotalCount, Is.EqualTo(1));
        Assert.That(list.PageSize, Is.EqualTo(1));
        Assert.That(list.Items.Single().Id, Is.EqualTo(first.Id));
        var empty = await Send(HttpMethod.Get, "/subscriptions?page=2147483647&pageSize=100", owner);
        Assert.That((await empty.Content.ReadFromJsonAsync<PageResponse<SubscriptionResponse>>())!.Items, Is.Empty);
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Put, HttpMethod.Delete })
        {
            var denied = await Send(method, $"/subscriptions/{first.Id}", foreign, method == HttpMethod.Put ? input : null);
            Assert.That(denied.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
        Assert.That((await Send(HttpMethod.Put, $"/subscriptions/{first.Id}", owner, input with { Price = "200.00" })).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Send(HttpMethod.Delete, $"/subscriptions/{first.Id}", owner)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await Send(HttpMethod.Delete, $"/subscriptions/{first.Id}", owner)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var read = await Send(HttpMethod.Get, $"/subscriptions/{first.Id}", owner);
        Assert.That((await read.Content.ReadFromJsonAsync<SubscriptionResponse>())!.Status, Is.EqualTo("archived"));
    }
    [Test]
    public async Task ConcurrentIdempotentRequestsCreateOnePaymentAndConflictOnChanges()
    {
        var token = await User();
        var sub = await Subscription(token);
        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Pay(token, sub.Id, "same-key")));
        foreach (var response in responses) Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
        var ids = new List<Guid>();
        foreach (var response in responses) ids.Add((await response.Content.ReadFromJsonAsync<PaymentResponse>())!.Id);
        Assert.That(ids.Distinct().Count(), Is.EqualTo(1));
        Assert.That((await Pay(token, sub.Id, "same-key", "200.00")).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>();
        Assert.That(await db.Payments.CountAsync(), Is.EqualTo(1));
        Assert.That(await db.IdempotencyRecords.CountAsync(), Is.EqualTo(1));
    }
    [Test]
    public async Task KeyCanBeReusedAtExactly24HoursAndIsIndependentForOtherUsers()
    {
        var token = await User();
        var sub = await Subscription(token);
        var first = await Pay(token, sub.Id, "key");
        var firstId = (await first.Content.ReadFromJsonAsync<PaymentResponse>())!.Id;
        clock.UtcNow = clock.UtcNow.AddHours(24);
        token = (await Login("user@example.com")).AccessToken;
        var second = await Pay(token, sub.Id, "key", "200.00");
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That((await second.Content.ReadFromJsonAsync<PaymentResponse>())!.Id, Is.Not.EqualTo(firstId));
        var other = await User("other@example.com");
        var otherSub = await Subscription(other);
        Assert.That((await Pay(other, otherSub.Id, "key")).StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }
    [Test]
    public async Task HistoryAndExpensesPreserveArchivedPaymentsAndPeriodBounds()
    {
        var token = await User();
        var sub = await Subscription(token);
        await Pay(token, sub.Id, "one", "100.00", "2026-11-25");
        await Pay(token, sub.Id, "two", "200.00", "2026-11-26");
        await Pay(token, sub.Id, "end", "400.00", "2026-11-27");
        var dollar = await Subscription(token, input with { Currency = "USD" });
        Assert.That((await Send(HttpMethod.Post, $"/subscriptions/{dollar.Id}/payments", token,
            new PaymentInput("10.00", "USD", "2026-11-25"), "usd")).StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That((await Send(HttpMethod.Put, $"/subscriptions/{sub.Id}", token, input with { Currency = "USD" })).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await Send(HttpMethod.Delete, $"/subscriptions/{sub.Id}", token);
        var response = await Send(HttpMethod.Get, "/analytics/expenses?from=2026-11-25&to=2026-11-27", token);
        var totals = (await response.Content.ReadFromJsonAsync<ExpensesResponse>())!;
        Assert.That(totals.ByCurrency.Select(x => x.Amount), Is.EqualTo(new[] { "300.00", "10.00" }));
        var history = await Send(HttpMethod.Get, $"/payments?from=2026-11-25&to=2026-11-27&subscriptionId={sub.Id}&pageSize=1", token);
        var list = (await history.Content.ReadFromJsonAsync<PageResponse<PaymentResponse>>())!;
        Assert.That(list.TotalCount, Is.EqualTo(2));
        Assert.That(list.Items.Single().Amount, Is.EqualTo("200.00"));
    }
    [Test]
    public async Task ForeignPaymentAndForeignHistoryAreNotFound()
    {
        var token = await User();
        var sub = await Subscription(token);
        var other = await User("other@example.com");
        Assert.That((await Pay(other, sub.Id, "key")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await Send(HttpMethod.Get, $"/payments?from=2026-11-01&to=2026-12-01&subscriptionId={sub.Id}", other)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var totals = await Send(HttpMethod.Get, "/analytics/expenses?from=2026-11-01&to=2026-12-01", other);
        Assert.That((await totals.Content.ReadFromJsonAsync<ExpensesResponse>())!.ByCurrency, Is.Empty);
    }
    [Test]
    public async Task MalformedAndUnknownFieldsReturnUnifiedValidationErrors()
    {
        var token = await User();
        var invalid = await Send(HttpMethod.Post, "/subscriptions", token, input with { Price = "1.001" });
        Assert.That(invalid.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var json = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("errors").TryGetProperty("price", out _), Is.True);
        var unknown = await Send(HttpMethod.Post, "/subscriptions", token, new { ownerId = Guid.NewGuid(), name = "Music" });
        Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Send(HttpMethod.Get, "/subscriptions/bad-guid", token)).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Send(HttpMethod.Get, "/subscriptions?pageSize=101", token)).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Send(HttpMethod.Get, "/payments", token)).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    [Test]
    public async Task DataAndTokensSurviveApplicationRestart()
    {
        var token = await User();
        var sub = await Subscription(token);
        await Pay(token, sub.Id, "key");
        client.Dispose(); factory.Dispose();
        factory = new(database, clock);
        client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.That((await Send(HttpMethod.Get, $"/subscriptions/{sub.Id}", token)).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var history = await Send(HttpMethod.Get, "/payments?from=2026-11-01&to=2026-12-01", token);
        Assert.That((await history.Content.ReadFromJsonAsync<PageResponse<PaymentResponse>>())!.TotalCount, Is.EqualTo(1));
    }
    [Test]
    public async Task TransactionRollbackAndForeignKeysPreventPartialWrites()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>();
            var atomic = new AtomicExecutor(db);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await atomic.ExecuteAsync<bool>(async () =>
            {
                db.Users.Add(new User { Id = Guid.NewGuid(), Email = "a", NormalizedEmail = "a", PasswordHash = "hash" });
                await db.SaveChangesAsync();
                throw new InvalidOperationException("Injected failure");
            }, default));
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>();
            Assert.That(await db.Users.CountAsync(), Is.Zero);
            db.Payments.Add(new Payment { Id = Guid.NewGuid(), SubscriptionId = Guid.NewGuid(), AmountMinor = 100, Currency = "RUB", PaidOn = new(2026, 11, 27) });
            Assert.ThrowsAsync<DbUpdateException>(async () => await db.SaveChangesAsync());
        }
    }
}
