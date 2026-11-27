namespace Subscriptions.Business.Contracts;

public sealed record SubscriptionExpense(Guid SubscriptionId, string Name, string Amount);
