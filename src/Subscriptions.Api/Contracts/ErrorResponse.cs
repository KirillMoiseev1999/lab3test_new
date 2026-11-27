using System.Text.Json.Serialization;

namespace Subscriptions.Api.Contracts;

public sealed record ErrorResponse(string Code, string Message, string RequestId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Errors = null);
