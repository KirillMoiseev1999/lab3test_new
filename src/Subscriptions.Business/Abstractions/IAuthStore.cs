using Subscriptions.Business.Models;
namespace Subscriptions.Business.Abstractions;

public interface IAuthStore
{
    Task<UserData?> FindUserAsync(string email, CancellationToken ct);
    Task AddUserAsync(UserData user, CancellationToken ct);
    Task<long> CountFailuresAsync(string email, DateTimeOffset cutoff, CancellationToken ct);
    Task AddFailureAsync(string email, DateTimeOffset now, CancellationToken ct);
    Task AddSessionAsync(SessionData session, CancellationToken ct);
    Task<SessionData?> FindSessionAsync(string hash, CancellationToken ct);
    Task RemoveSessionAsync(string hash, CancellationToken ct);
}
