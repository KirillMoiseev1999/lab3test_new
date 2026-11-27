namespace Subscriptions.Business.Models;

public sealed record UserData(Guid Id, string Email, string NormalizedEmail, string PasswordHash);
