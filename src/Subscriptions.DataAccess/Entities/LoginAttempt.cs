namespace Subscriptions.DataAccess.Entities;

public sealed class LoginAttempt
{
    public Guid Id { get; set; }
    public string NormalizedEmail { get; set; } = "";
    public long OccurredAtUtcTicks { get; set; }
}
