namespace Subscriptions.Business.Contracts;

public sealed record ExpensesResponse(string Total, IReadOnlyList<SubscriptionExpense> BySubscription);
