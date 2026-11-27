using Subscriptions.Business.Abstractions;
using Subscriptions.Business.Contracts;
using Subscriptions.Business.Errors;
using Subscriptions.Business.Models;
using Subscriptions.Business.Validation;
namespace Subscriptions.Business.Services;


public sealed class ExpenseService(IPaymentStore store)
{
    public async Task<ExpensesResponse> GetAsync(Guid user, string? from, string? to, CancellationToken ct = default)
    {
        var period = InputValidator.Period(from, to);
        var rows = await store.ExpensesAsync(user, period.From, period.To, ct);
        var currencies = rows.GroupBy(x => x.Currency, StringComparer.Ordinal)
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(g => new CurrencyTotal(g.Key, Format(g.Sum(x => x.Amount)))).ToArray();
        var categories = rows.GroupBy(x => (x.Category, x.Currency))
            .OrderBy(x => x.Key.Category, StringComparer.Ordinal).ThenBy(x => x.Key.Currency, StringComparer.Ordinal)
            .Select(g => new CategoryTotal(g.Key.Category, g.Key.Currency, Format(g.Sum(x => x.Amount)))).ToArray();
        return new(currencies, categories);
    }
    private static string Format(decimal amount) => amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
}
