namespace Subscriptions.DataAccess.Entities;

public sealed class Payment
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "";
    public DateOnly PaidOn { get; set; }
    public long CreatedAtUtcTicks { get; set; }
}
