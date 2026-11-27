namespace Subscriptions.Business.Errors;


public sealed class BusinessException : Exception
{
    public int Status { get; }
    public string Code { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public BusinessException(int status, string code, string message,
        IReadOnlyDictionary<string, string[]>? errors = null) : base(message)
    {
        Status = status;
        Code = code;
        Errors = errors;
    }
}
