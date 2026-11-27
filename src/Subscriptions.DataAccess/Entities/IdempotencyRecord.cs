namespace Subscriptions.DataAccess.Entities;

public sealed class IdempotencyRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Key { get; set; } = "";
    public Guid PaymentId { get; set; }
    public long CreatedAtUtcTicks { get; set; }
}
