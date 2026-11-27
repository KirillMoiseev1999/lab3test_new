using Moq;
using NUnit.Framework;
using Subscriptions.Business.Services;
using Subscriptions.Business.Models;

namespace Subscriptions.Business.Tests;

public class ExpenseServiceTests : ServiceFixture
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task ExpensesSeparateCurrenciesAndUseCurrentCategory(bool empty)
    {
        var rows = empty ? Array.Empty<ExpenseData>() : new[]
        {
            new ExpenseData("Music","RUB",100m),new ExpenseData("Music","RUB",200m),
            new ExpenseData("Other","USD",10m),new ExpenseData("Other","RUB",0.01m)
        };
        Payments.Setup(x => x.ExpensesAsync(User, new(2026, 11, 1), new(2026, 12, 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);
        var result = await new ExpenseService(Payments.Object).GetAsync(User, "2026-11-01", "2026-12-01");
        if (empty)
        {
            Assert.That(result.ByCurrency, Is.Empty);
            Assert.That(result.ByCategory, Is.Empty);
        }
        else
        {
            Assert.That(result.ByCurrency.Select(x => x.Amount), Is.EqualTo(new[] { "300.01", "10.00" }));
            Assert.That(result.ByCategory.Select(x => x.Amount), Is.EqualTo(new[] { "300.00", "0.01", "10.00" }));
        }
    }
    [Test]
    public void InvalidPeriodIsRejected() =>
        Fails(async () => await new ExpenseService(Payments.Object).GetAsync(User, "2026-12-01", "2026-11-01"), 400);
}
