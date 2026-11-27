namespace Subscriptions.Business.Models;

public sealed record SubscriptionData(Guid Id, Guid UserId, string Name, string Category,
    decimal Price, string Currency, string Frequency, DateOnly NextPaymentDate, string Status);
