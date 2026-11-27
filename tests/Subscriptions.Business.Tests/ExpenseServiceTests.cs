using Moq;
using NUnit.Framework;
using Subscriptions.Business.Services;
using Subscriptions.Business.Models;

namespace Subscriptions.Business.Tests;

public class ExpenseServiceTests : ServiceFixture
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task ExpensesSumActualPaymentsBySubscription(bool empty)
    {
        var second = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var rows = empty ? Array.Empty<ExpenseData>() : new[]
        {
            new ExpenseData(second, "Other", 0.01m),
            new ExpenseData(Id, "Music renamed", 100m),
            new ExpenseData(Id, "Music renamed", 200m)
        };
        Payments.Setup(x => x.ExpensesAsync(User, new(2026, 11, 1), new(2026, 12, 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);
        var result = await new ExpenseService(Payments.Object).GetAsync(User, "2026-11-01", "2026-12-01");
        if (empty)
        {
            Assert.That(result.Total, Is.EqualTo("0.00"));
            Assert.That(result.BySubscription, Is.Empty);
        }
        else
        {
            Assert.That(result.Total, Is.EqualTo("300.01"));
            Assert.That(result.BySubscription.Select(x => x.SubscriptionId), Is.EqualTo(new[] { Id, second }));
            Assert.That(result.BySubscription.Select(x => x.Amount), Is.EqualTo(new[] { "300.00", "0.01" }));
            Assert.That(result.BySubscription.First().Name, Is.EqualTo("Music renamed"));
        }
    }
    [Test]
    public void InvalidPeriodIsRejected() =>
        Fails(async () => await new ExpenseService(Payments.Object).GetAsync(User, "2026-12-01", "2026-11-01"), 400);
}
