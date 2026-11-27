namespace Subscriptions.Business.Models;

public sealed record IdempotencyData(Guid Id, Guid UserId, string Key,
    PaymentData Payment, DateTimeOffset CreatedAtUtc);
