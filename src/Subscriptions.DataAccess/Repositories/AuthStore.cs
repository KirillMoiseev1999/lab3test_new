using Microsoft.EntityFrameworkCore;
using Subscriptions.Business.Abstractions;
using Subscriptions.Business.Models;
using Subscriptions.DataAccess.Entities;
namespace Subscriptions.DataAccess.Repositories;


public sealed class AuthStore(SubscriptionsDbContext db) : IAuthStore
{
    public async Task<UserData?> FindUserAsync(string email, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.NormalizedEmail == email, ct);
        return user is null ? null : new(user.Id, user.Email, user.NormalizedEmail, user.PasswordHash);
    }
    public Task AddUserAsync(UserData user, CancellationToken ct)
    {
        db.Users.Add(new User { Id = user.Id, Email = user.Email, NormalizedEmail = user.NormalizedEmail, PasswordHash = user.PasswordHash });
        return Task.CompletedTask;
    }
    public Task AddSessionAsync(SessionData session, CancellationToken ct)
    {
        db.Sessions.Add(new Session { TokenHash = session.TokenHash, UserId = session.UserId, ExpiresAtUtcTicks = session.ExpiresAtUtc.UtcTicks });
        return Task.CompletedTask;
    }
    public async Task<SessionData?> FindSessionAsync(string hash, CancellationToken ct)
    {
        var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        return session is null ? null : new(session.TokenHash, session.UserId, new DateTimeOffset(session.ExpiresAtUtcTicks, TimeSpan.Zero));
    }
    public async Task RemoveSessionAsync(string hash, CancellationToken ct)
    {
        var session = await db.Sessions.SingleAsync(x => x.TokenHash == hash, ct);
        db.Sessions.Remove(session);
    }
}
