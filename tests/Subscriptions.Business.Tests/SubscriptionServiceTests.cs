using NUnit.Framework;
using Moq;
using Subscriptions.Business.Models;

namespace Subscriptions.Business.Tests;

public class SubscriptionServiceTests : ServiceFixture
{
    [Test]
    public async Task CreateTrimsStringsAndMapsResponse()
    {
        SubscriptionData? stored = null;
        Subs.Setup(x => x.AddAsync(It.IsAny<SubscriptionData>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionData, CancellationToken>((data, _) => stored = data).Returns(Task.CompletedTask);
        var result = await SubService.CreateAsync(User, Input with { Name = " Music " });
        Assert.That(result.Name, Is.EqualTo("Music"));
        Assert.That(result.Price, Is.EqualTo("100.00"));
        Assert.That(result.NextPaymentDate, Is.EqualTo("2026-12-01"));
        Assert.That(stored!.UserId, Is.EqualTo(User));
    }
    [Test]
    public async Task GetUsesOwnerAndMapsAllFields()
    {
        var result = await SubService.GetAsync(User, Id);
        Assert.That(result.Id, Is.EqualTo(Id));
        Assert.That(result.Frequency, Is.EqualTo("monthly"));
        Assert.That(result.Status, Is.EqualTo("active"));
    }
    [Test]
    public void MissingOrForeignSubscriptionReturns404()
    {
        Subs.Setup(x => x.FindAsync(User, Id, It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionData?)null);
        Fails(async () => await SubService.GetAsync(User, Id), 404);
        Fails(async () => await SubService.UpdateAsync(User, Id, Input), 404);
        Fails(async () => await SubService.ArchiveAsync(User, Id), 404);
    }
    [TestCase("active")]
    [TestCase("paused")]
    [TestCase("archived")]
    public async Task UpdateReplacesEditableFields(string status)
    {
        var result = await SubService.UpdateAsync(User, Id, Input with { Name = "New", Price = "200.00", Status = status });
        Assert.That(result.Name, Is.EqualTo("New"));
        Assert.That(result.Price, Is.EqualTo("200.00"));
        Assert.That(result.Status, Is.EqualTo(status));
    }
    [Test]
    public async Task ArchiveOnlyChangesStatus()
    {
        await SubService.ArchiveAsync(User, Id);
        var archived = Data with { Status = "archived" };
        Subs.Verify(x => x.UpdateAsync(archived, It.IsAny<CancellationToken>()), Times.Once);
    }
    [TestCase(null)]
    [TestCase("active")]
    public async Task ListForwardsFiltersAndPagination(string? status)
    {
        Subs.Setup(x => x.ListAsync(User, status, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { Data }, 11));
        var result = await SubService.ListAsync(User, status, "2", "5");
        Assert.That(result.Page, Is.EqualTo(2));
        Assert.That(result.PageSize, Is.EqualTo(5));
        Assert.That(result.TotalCount, Is.EqualTo(11));
        Assert.That(result.Items.Single().Id, Is.EqualTo(Id));
    }
    [Test]
    public void ListRejectsUnknownStatus() =>
        Fails(async () => await SubService.ListAsync(User, "invalid", null, null), 400);
    [Test]
    public void CreateRejectsInvalidName() =>
        Fails(async () => await SubService.CreateAsync(User, Input with { Name = "" }), 400);
}
