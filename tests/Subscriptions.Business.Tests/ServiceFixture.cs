using Moq;
using NUnit.Framework;
using Subscriptions.Business.Abstractions;
using Subscriptions.Business.Contracts;
using Subscriptions.Business.Errors;
using Subscriptions.Business.Models;
using Subscriptions.Business.Services;

namespace Subscriptions.Business.Tests;

public abstract class ServiceFixture
{
    protected readonly Guid User = Guid.Parse("11111111-1111-1111-1111-111111111111");
    protected readonly Guid Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    protected Mock<IAuthStore> Auth = null!;
    protected Mock<ISubscriptionStore> Subs = null!;
    protected Mock<IPaymentStore> Payments = null!;
    protected Mock<IAtomicExecutor> Atomic = null!;
    protected Mock<IPasswordService> Passwords = null!;
    protected Mock<ITokenService> Tokens = null!;
    protected Mock<IClock> Clock = null!;
    protected DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
    protected SubscriptionService SubService = null!;
    protected SubscriptionData Data => new(Id, User, "Music", 100m, "monthly", new(2026, 12, 1), "active");
    protected SubscriptionInput Input => new("Music", "100.00", "monthly", "2026-12-01", "active");
    protected PaymentInput PaymentInput => new("100.00", "2026-11-27");
    protected PaymentData Payment => new(Id, Id, 100m, new(2026, 11, 27), Now);
    protected AuthService AuthService => new(Auth.Object, Atomic.Object, Passwords.Object, Tokens.Object, Clock.Object);
    protected PaymentService PaymentService => new(Payments.Object, SubService, Atomic.Object, Clock.Object);

    [SetUp]
    public void Setup()
    {
        Auth = new(); Subs = new(); Payments = new(); Atomic = new();
        Passwords = new(); Tokens = new(); Clock = new();
        Clock.SetupGet(x => x.UtcNow).Returns(() => Now);
        Passwords.Setup(x => x.Hash(It.IsAny<Guid>(), It.IsAny<string>())).Returns("password-hash");
        Tokens.Setup(x => x.Create()).Returns(new string('A', 64));
        Tokens.Setup(x => x.Hash(It.IsAny<string>())).Returns("hash");
        Subs.Setup(x => x.FindAsync(User, Id, It.IsAny<CancellationToken>())).ReturnsAsync(() => Data);
        RunAtomic<UserResponse>(); RunAtomic<LoginResponse>(); RunAtomic<bool>();
        RunAtomic<SubscriptionResponse>(); RunAtomic<PaymentResponse>();
        SubService = new(Subs.Object, Atomic.Object);
    }
    private void RunAtomic<T>() => Atomic.Setup(x => x.ExecuteAsync(It.IsAny<Func<Task<T>>>(), It.IsAny<CancellationToken>()))
        .Returns<Func<Task<T>>, CancellationToken>((work, _) => work());
    protected static BusinessException Fails(AsyncTestDelegate action, int status)
    {
        var ex = Assert.ThrowsAsync<BusinessException>(action)!;
        Assert.That(ex.Status, Is.EqualTo(status));
        return ex;
    }
}
