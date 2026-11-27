using Subscriptions.Business.Abstractions;
namespace Subscriptions.Api.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
