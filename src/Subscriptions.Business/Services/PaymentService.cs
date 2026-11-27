using Subscriptions.Business.Abstractions;
using Subscriptions.Business.Contracts;
using Subscriptions.Business.Errors;
using Subscriptions.Business.Models;
using Subscriptions.Business.Validation;
namespace Subscriptions.Business.Services;


public sealed class PaymentService(IPaymentStore store, SubscriptionService subscriptions,
    IAtomicExecutor atomic, IClock clock)
{
    public Task<PaymentResponse> CreateAsync(Guid user, Guid subscriptionId, string? key,
        PaymentInput input, CancellationToken ct = default)
    {
        var normalizedKey = InputValidator.Required(key, "Idempotency-Key", 128);
        var amount = InputValidator.Money(input.Amount, "amount");
        var paidOn = InputValidator.Date(input.PaidOn, "paidOn");
        if (paidOn > DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
            throw InputValidator.Invalid("paidOn", "Дата платежа не может быть в будущем.");
        return atomic.ExecuteAsync(async () =>
        {
            await subscriptions.OwnedAsync(user, subscriptionId, ct);
            var previous = await store.FindKeyAsync(user, normalizedKey, ct);
            var now = clock.UtcNow;
            if (previous is not null && previous.CreatedAtUtc.AddHours(24) > now)
            {
                var payment = previous.Payment;
                if (payment.SubscriptionId != subscriptionId || payment.Amount != amount ||
                    payment.PaidOn != paidOn)
                    throw new BusinessException(409, "idempotency_conflict", "Ключ использован с другими данными.");
                return Map(payment);
            }
            var data = new PaymentData(Guid.NewGuid(), subscriptionId, amount, paidOn, now);
            var record = new IdempotencyData(previous?.Id ?? Guid.NewGuid(), user, normalizedKey, data, now);
            await store.SaveAsync(data, record, ct);
            return Map(data);
        }, ct);
    }
    public async Task<PageResponse<PaymentResponse>> ListAsync(Guid user, Guid? subscriptionId,
        string? from, string? to, string? page, string? size, CancellationToken ct = default)
    {
        var period = InputValidator.Period(from, to);
        var paging = InputValidator.Paging(page, size);
        if (subscriptionId is not null) await subscriptions.OwnedAsync(user, subscriptionId.Value, ct);
        var result = await store.ListAsync(user, subscriptionId, period.From, period.To, paging.Page, paging.Size, ct);
        return new(result.Items.Select(Map).ToArray(), paging.Page, paging.Size, result.Count);
    }
    private static PaymentResponse Map(PaymentData data) =>
        new(data.Id, data.SubscriptionId, InputValidator.Amount(data.Amount),
            InputValidator.DateText(data.PaidOn));
}
