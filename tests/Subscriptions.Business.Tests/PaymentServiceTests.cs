using NUnit.Framework;
using Moq;
using Subscriptions.Business.Models;

namespace Subscriptions.Business.Tests;

public class PaymentServiceTests : ServiceFixture
{
    [Test]
    public async Task NewPaymentSavesNormalizedKeyAndMapsResponse()
    {
        var result = await PaymentService.CreateAsync(User, Id, " key ", PaymentInput);
        Assert.That(result.Amount, Is.EqualTo("100.00"));
        Assert.That(result.PaidOn, Is.EqualTo("2026-11-27"));
        Assert.That(result.SubscriptionId, Is.EqualTo(Id));
        Payments.Verify(x => x.SaveAsync(It.Is<PaymentData>(p => p.Amount == 100m && p.CreatedAtUtc == Now),
            It.Is<IdempotencyData>(r => r.Key == "key" && r.UserId == User), It.IsAny<CancellationToken>()), Times.Once);
    }
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1)]
    public async Task ReplayWindowHasExact24HourBoundary(int secondsFromExpiry)
    {
        var created = Now.AddHours(-24).AddSeconds(-secondsFromExpiry);
        Payments.Setup(x => x.FindKeyAsync(User, "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyData(Id, User, "key", Payment, created));
        var result = await PaymentService.CreateAsync(User, Id, "key", PaymentInput);
        if (secondsFromExpiry < 0)
        {
            Assert.That(result.Id, Is.EqualTo(Payment.Id));
            Payments.Verify(x => x.SaveAsync(It.IsAny<PaymentData>(), It.IsAny<IdempotencyData>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        else Payments.Verify(x => x.SaveAsync(It.IsAny<PaymentData>(), It.Is<IdempotencyData>(r => r.Id == Id), It.IsAny<CancellationToken>()), Times.Once);
    }
    [TestCase("subscription")]
    [TestCase("amount")]
    [TestCase("date")]
    public void ChangedReplayDetailsConflict(string field)
    {
        var old = Payment with
        {
            SubscriptionId = field == "subscription" ? Guid.NewGuid() : Id,
            Amount = field == "amount" ? 200m : 100m,
            PaidOn = field == "date" ? new DateOnly(2026, 11, 26) : new DateOnly(2026, 11, 27)
        };
        Payments.Setup(x => x.FindKeyAsync(User, "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyData(Id, User, "key", old, Now));
        Fails(async () => await PaymentService.CreateAsync(User, Id, "key", PaymentInput), 409);
    }
    [Test]
    public void FuturePaymentIsRejected() =>
        Fails(async () => await PaymentService.CreateAsync(User, Id, "key", PaymentInput with { PaidOn = "2026-11-28" }), 400);
    [TestCase("active")]
    [TestCase("paused")]
    [TestCase("archived")]
    public async Task PaymentCanDifferFromPriceWithoutChangingSubscription(string status)
    {
        Subs.Setup(x => x.FindAsync(User, Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Data with { Status = status });
        var result = await PaymentService.CreateAsync(User, Id, "key", PaymentInput with { Amount = "0.01" });
        Assert.That(result.Amount, Is.EqualTo("0.01"));
        Subs.Verify(x => x.UpdateAsync(It.IsAny<SubscriptionData>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Test]
    public async Task ReplayTreatsEquivalentDecimalStringsAsSameAmount()
    {
        Payments.Setup(x => x.FindKeyAsync(User, "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyData(Id, User, "key", Payment, Now));
        var result = await PaymentService.CreateAsync(User, Id, " key ", PaymentInput with { Amount = "100" });
        Assert.That(result.Id, Is.EqualTo(Payment.Id));
        Payments.Verify(x => x.SaveAsync(It.IsAny<PaymentData>(), It.IsAny<IdempotencyData>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Test]
    public void ForeignSubscriptionIsRejected()
    {
        Subs.Setup(x => x.FindAsync(User, Id, It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionData?)null);
        Fails(async () => await PaymentService.CreateAsync(User, Id, "key", PaymentInput), 404);
    }
    [TestCase(true)]
    [TestCase(false)]
    public async Task HistoryUsesOwnerPeriodAndOptionalSubscription(bool filtered)
    {
        Guid? subscription = filtered ? Id : null;
        Payments.Setup(x => x.ListAsync(User, subscription, new(2026, 11, 1), new(2026, 12, 1), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { Payment }, 1));
        var result = await PaymentService.ListAsync(User, subscription, "2026-11-01", "2026-12-01", null, null);
        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(result.Items.Single().Id, Is.EqualTo(Id));
    }
    [Test]
    public void MissingKeyIsRejected() =>
        Fails(async () => await PaymentService.CreateAsync(User, Id, null, PaymentInput), 400);
}
