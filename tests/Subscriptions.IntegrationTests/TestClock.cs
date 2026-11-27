using Subscriptions.Business.Abstractions;
namespace Subscriptions.IntegrationTests;

public sealed class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
}
