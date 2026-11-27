namespace Subscriptions.Business.Models;

public sealed record SessionData(string TokenHash, Guid UserId, DateTimeOffset ExpiresAtUtc);
