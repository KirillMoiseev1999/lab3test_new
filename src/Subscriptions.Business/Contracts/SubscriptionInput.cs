namespace Subscriptions.Business.Contracts;

public sealed record SubscriptionInput(string? Name, string? Category, string? Price,
    string? Currency, string? Frequency, string? NextPaymentDate, string? Status);
