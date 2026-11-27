namespace Subscriptions.Business.Contracts;

public sealed record SubscriptionResponse(Guid Id, string Name, string Price,
    string Frequency, string NextPaymentDate, string Status);
