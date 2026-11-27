namespace Subscriptions.DataAccess.Entities;

public sealed class Subscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public long PriceMinor { get; set; }
    public string Currency { get; set; } = "";
    public string Frequency { get; set; } = "";
    public DateOnly NextPaymentDate { get; set; }
    public string Status { get; set; } = "";
}
