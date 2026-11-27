using NUnit.Framework;
using Moq;
using Subscriptions.Business.Contracts;
using Subscriptions.Business.Models;

namespace Subscriptions.Business.Tests;

public class AuthServiceTests : ServiceFixture
{
    private AuthInput Credentials => new(" User@Example.com ", "long-password-2026");

    [Test]
    public async Task RegisterNormalizesEmailAndHashesPassword()
    {
        UserData? stored = null;
        Auth.Setup(x => x.AddUserAsync(It.IsAny<UserData>(), It.IsAny<CancellationToken>()))
            .Callback<UserData, CancellationToken>((value, _) => stored = value).Returns(Task.CompletedTask);
        var result = await AuthService.RegisterAsync(Credentials);
        Assert.That(stored!.NormalizedEmail, Is.EqualTo("user@example.com"));
        Assert.That(stored.PasswordHash, Is.EqualTo("password-hash"));
        Assert.That(result.Email, Is.EqualTo("User@Example.com"));
        Assert.That(result.Id, Is.EqualTo(stored.Id));
        Passwords.Verify(x => x.Hash(stored.Id, Credentials.Password!), Times.Once);
    }
    [Test]
    public void DuplicateRegistrationConflicts()
    {
        Auth.Setup(x => x.FindUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserData(User, "email", "email", "hash"));
        Fails(async () => await AuthService.RegisterAsync(Credentials), 409);
        Auth.Verify(x => x.AddUserAsync(It.IsAny<UserData>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Test]
    public async Task LoginIssuesThirtyMinuteSession()
    {
        Auth.Setup(x => x.FindUserAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserData(User, "User@example.com", "user@example.com", "hash"));
        Passwords.Setup(x => x.Verify(User, "hash", Credentials.Password!)).Returns(true);
        var result = await AuthService.LoginAsync(Credentials);
        Assert.That(result.TokenType, Is.EqualTo("Bearer"));
        Assert.That(result.ExpiresAt, Is.EqualTo(Now.AddMinutes(30)));
        Assert.That(result.AccessToken.Length, Is.EqualTo(64));
        Auth.Verify(x => x.AddSessionAsync(new SessionData("hash", User, Now.AddMinutes(30)), It.IsAny<CancellationToken>()), Times.Once);
    }
    [TestCase(true)]
    [TestCase(false)]
    public void BadCredentialsDoNotRevealAccountOrCreateSession(bool exists)
    {
        if (exists) Auth.Setup(x => x.FindUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserData(User, "email", "email", "hash"));
        var ex = Fails(async () => await AuthService.LoginAsync(Credentials), 401);
        Assert.That(ex.Code, Is.EqualTo("invalid_session"));
        Auth.Verify(x => x.AddSessionAsync(It.IsAny<SessionData>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    [TestCase("bad")]
    [TestCase("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
    public void InvalidTokenFormatIsRejected(string? token) =>
        Fails(async () => await AuthService.AuthenticateAsync(token), 401);

    [TestCase(-1)]
    [TestCase(0)]
    public void ExpiredSessionIsRejected(int seconds)
    {
        Auth.Setup(x => x.FindSessionAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionData("hash", User, Now.AddSeconds(seconds)));
        Fails(async () => await AuthService.AuthenticateAsync(new string('A', 64)), 401);
    }
    [Test]
    public void MissingSessionIsRejected() =>
        Fails(async () => await AuthService.AuthenticateAsync(new string('A', 64)), 401);
    [Test]
    public async Task ValidSessionCanBeAuthenticatedAndRevoked()
    {
        Auth.Setup(x => x.FindSessionAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionData("hash", User, Now.AddSeconds(1)));
        Assert.That(await AuthService.AuthenticateAsync(new string('A', 64)), Is.EqualTo(User));
        Assert.That(await AuthService.LogoutAsync(new string('A', 64)), Is.True);
        Auth.Verify(x => x.RemoveSessionAsync("hash", It.IsAny<CancellationToken>()), Times.Once);
    }
    [Test]
    public void InvalidEmailStopsRegistrationBeforeStorage() =>
        Fails(async () => await AuthService.RegisterAsync(new("bad", "long-password")), 400);
}
