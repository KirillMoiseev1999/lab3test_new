namespace Subscriptions.Business.Abstractions;

public interface IReadinessProbe
{
    Task<bool> IsReadyAsync(CancellationToken ct);
}
