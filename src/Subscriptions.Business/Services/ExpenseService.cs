using Subscriptions.Business.Abstractions;
using Subscriptions.Business.Contracts;
using Subscriptions.Business.Validation;

namespace Subscriptions.Business.Services;

public sealed class ExpenseService(IPaymentStore store)
{
    public async Task<ExpensesResponse> GetAsync(Guid user, string? from, string? to, CancellationToken ct = default)
    {
        var period = InputValidator.Period(from, to);
        var rows = await store.ExpensesAsync(user, period.From, period.To, ct);
        var total = InputValidator.Amount(rows.Sum(x => x.Amount));
        var subscriptions = rows.GroupBy(x => x.SubscriptionId)
            .OrderBy(group => group.Key.ToString("D"), StringComparer.Ordinal)
            .Select(group => new SubscriptionExpense(group.Key, group.First().Name,
                InputValidator.Amount(group.Sum(x => x.Amount))))
            .ToArray();
        return new(total, subscriptions);
    }
}
