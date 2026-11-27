namespace Subscriptions.Api.Contracts;

public sealed record ErrorResponse(string Code, string Message, string RequestId,
    IReadOnlyDictionary<string, string[]>? Errors = null);
