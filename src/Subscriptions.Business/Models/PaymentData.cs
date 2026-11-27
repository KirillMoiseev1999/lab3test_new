namespace Subscriptions.Business.Models;

public sealed record PaymentData(Guid Id, Guid SubscriptionId, decimal Amount,
    string Currency, DateOnly PaidOn, DateTimeOffset CreatedAtUtc);
