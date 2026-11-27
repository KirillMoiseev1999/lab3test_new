namespace Subscriptions.Business.Contracts;

public sealed record PaymentInput(string? Amount, string? Currency, string? PaidOn);
