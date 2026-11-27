namespace Subscriptions.Business.Models;

public sealed record ExpenseData(Guid SubscriptionId, string Name, decimal Amount);
