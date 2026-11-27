using NUnit.Framework;
using Subscriptions.Business.Validation;
using Subscriptions.Business.Errors;

namespace Subscriptions.Business.Tests;

public class InputValidatorTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("xxxx")]
    public void RequiredRejectsMissingOrLongStrings(string? value) =>
        Assert.Throws<BusinessException>(() => InputValidator.Required(value, "name", 3));
    [Test]
    public void RequiredTrimsText() => Assert.That(InputValidator.Required(" abc ", "name", 3), Is.EqualTo("abc"));
    [TestCase("bad")]
    [TestCase("Name <a@b.com>")]
    public void EmailRejectsInvalidAddress(string email) =>
        Assert.Throws<BusinessException>(() => InputValidator.Email(email));
    [TestCase(0)]
    [TestCase(11)]
    [TestCase(129)]
    public void PasswordRejectsInvalidLength(int length) =>
        Assert.Throws<BusinessException>(() => InputValidator.Password(new string('a', length)));
    [Test]
    public void PasswordRejectsNull() => Assert.Throws<BusinessException>(() => InputValidator.Password(null));
    [TestCase(12)]
    [TestCase(128)]
    public void PasswordAcceptsBoundaries(int length) =>
        Assert.That(InputValidator.Password(new string('a', length)).Length, Is.EqualTo(length));
    [TestCase(null)]
    [TestCase("")]
    [TestCase("-1")]
    [TestCase("0")]
    [TestCase("0.00")]
    [TestCase("1.001")]
    [TestCase("1000000000.00")]
    [TestCase("1,50")]
    [TestCase(" 1.00")]
    [TestCase("1.00\n")]
    public void MoneyRejectsInvalidInput(string? value) =>
        Assert.Throws<BusinessException>(() => InputValidator.Money(value, "amount"));
    [TestCase("0.01", 1)]
    [TestCase("1", 100)]
    [TestCase("1.5", 150)]
    [TestCase("999999999.99", 99999999999L)]
    public void MoneyParsesExactly(string value, long minor) =>
        Assert.That(InputValidator.Money(value, "amount"), Is.EqualTo(minor / 100m));
    [TestCase(null)]
    [TestCase("other")]
    public void ChoiceRejectsUnknownValues(string? value) =>
        Assert.Throws<BusinessException>(() => InputValidator.Choice(value, "currency", "RUB", "USD"));
    [TestCase(null)]
    [TestCase("2026-02-29")]
    [TestCase("01.01.2026")]
    public void DateRejectsInvalidValues(string? value) =>
        Assert.Throws<BusinessException>(() => InputValidator.Date(value, "from"));
    [TestCase("2026-12-01", "2026-12-01")]
    [TestCase("2026-12-02", "2026-12-01")]
    public void PeriodRequiresIncreasingBounds(string from, string to) =>
        Assert.Throws<BusinessException>(() => InputValidator.Period(from, to));
    [TestCase("0", "20")]
    [TestCase("bad", "20")]
    [TestCase("1", "0")]
    [TestCase("1", "101")]
    [TestCase("1", "bad")]
    public void PagingRejectsInvalidValues(string page, string size) =>
        Assert.Throws<BusinessException>(() => InputValidator.Paging(page, size));
    [TestCase(null, null, 1, 20)]
    [TestCase("2", "100", 2, 100)]
    public void PagingAcceptsDefaultsAndBounds(string? page, string? size, int expectedPage, int expectedSize) =>
        Assert.That(InputValidator.Paging(page, size), Is.EqualTo((expectedPage, expectedSize)));
}
