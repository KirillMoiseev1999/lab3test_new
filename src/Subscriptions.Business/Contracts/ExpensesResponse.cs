namespace Subscriptions.Business.Contracts;

public sealed record ExpensesResponse(IReadOnlyList<CurrencyTotal> ByCurrency, IReadOnlyList<CategoryTotal> ByCategory);
