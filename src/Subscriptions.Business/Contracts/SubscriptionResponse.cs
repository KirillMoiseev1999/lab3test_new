namespace Subscriptions.Business.Contracts;

public sealed record SubscriptionResponse(Guid Id, string Name, string Category, string Price,
    string Currency, string Frequency, string NextPaymentDate, string Status);
