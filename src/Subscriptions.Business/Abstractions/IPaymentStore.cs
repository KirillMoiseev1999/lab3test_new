using Subscriptions.Business.Models;
namespace Subscriptions.Business.Abstractions;

public interface IPaymentStore
{
    Task<IdempotencyData?> FindKeyAsync(Guid userId, string key, CancellationToken ct);
    Task SaveAsync(PaymentData payment, IdempotencyData record, CancellationToken ct);
    Task<(IReadOnlyList<PaymentData> Items, int Count)> ListAsync(Guid userId, Guid? subscriptionId,
        DateOnly from, DateOnly to, int page, int size, CancellationToken ct);
    Task<IReadOnlyList<ExpenseData>> ExpensesAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct);
}
