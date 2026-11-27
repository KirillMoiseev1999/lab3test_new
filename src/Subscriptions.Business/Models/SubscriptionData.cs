namespace Subscriptions.Business.Models;

public sealed record SubscriptionData(Guid Id, Guid UserId, string Name, decimal Price, string Frequency, DateOnly NextPaymentDate, string Status);
