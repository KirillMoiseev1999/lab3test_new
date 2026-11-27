namespace Subscriptions.DataAccess.Entities;

public sealed class Session
{
    public string TokenHash { get; set; } = "";
    public Guid UserId { get; set; }
    public long ExpiresAtUtcTicks { get; set; }
}
