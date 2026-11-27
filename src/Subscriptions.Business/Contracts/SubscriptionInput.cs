namespace Subscriptions.Business.Contracts;

public sealed record SubscriptionInput(string? Name, string? Price,
    string? Frequency, string? NextPaymentDate, string? Status);
