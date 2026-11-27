namespace Subscriptions.Business.Contracts;

public sealed record PaymentResponse(Guid Id, Guid SubscriptionId, string Amount, string PaidOn);
