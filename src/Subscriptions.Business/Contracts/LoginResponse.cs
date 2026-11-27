namespace Subscriptions.Business.Contracts;

public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);
