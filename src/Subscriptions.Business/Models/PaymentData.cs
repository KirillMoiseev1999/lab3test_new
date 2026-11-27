namespace Subscriptions.Business.Models;

public sealed record PaymentData(Guid Id, Guid SubscriptionId, decimal Amount,
    DateOnly PaidOn, DateTimeOffset CreatedAtUtc);
