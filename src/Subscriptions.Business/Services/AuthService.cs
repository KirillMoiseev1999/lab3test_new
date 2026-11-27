using Subscriptions.Business.Abstractions;
using Subscriptions.Business.Contracts;
using Subscriptions.Business.Errors;
using Subscriptions.Business.Models;
using Subscriptions.Business.Validation;
namespace Subscriptions.Business.Services;


public sealed class AuthService(IAuthStore store, IAtomicExecutor atomic,
    IPasswordService passwords, ITokenService tokens, IClock clock)
{
    public Task<UserResponse> RegisterAsync(AuthInput input, CancellationToken ct = default)
    {
        var email = InputValidator.Email(input.Email);
        var password = InputValidator.Password(input.Password);
        var normalized = email.ToLowerInvariant();
        return atomic.ExecuteAsync(async () =>
        {
            if (await store.FindUserAsync(normalized, ct) is not null)
                throw new BusinessException(409, "email_already_registered", "Адрес уже зарегистрирован.");
            var id = Guid.NewGuid();
            await store.AddUserAsync(new(id, email, normalized, passwords.Hash(id, password)), ct);
            return new UserResponse(id, email);
        }, ct);
    }
    public async Task<LoginResponse> LoginAsync(AuthInput input, CancellationToken ct = default)
    {
        var email = InputValidator.Email(input.Email).ToLowerInvariant();
        var password = InputValidator.Password(input.Password);
        // Commit a failed attempt before returning the authentication error.
        var result = await atomic.ExecuteAsync<LoginResponse?>(async () =>
        {
            var now = clock.UtcNow;
            if (await store.CountFailuresAsync(email, now.AddMinutes(-15), ct) >= 5)
                throw new BusinessException(429, "too_many_login_attempts", "Повторите вход позже.");
            var user = await store.FindUserAsync(email, ct);
            if (user is null || !passwords.Verify(user.Id, user.PasswordHash, password))
            {
                await store.AddFailureAsync(email, now, ct);
                return null;
            }
            var token = tokens.Create();
            var expires = now.AddMinutes(30);
            await store.AddSessionAsync(new(tokens.Hash(token), user.Id, expires), ct);
            return new LoginResponse(token, "Bearer", expires);
        }, ct);
        return result ?? throw Unauthorized();
    }
    public async Task<Guid> AuthenticateAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length != 64 || !token.All(Uri.IsHexDigit))
            throw Unauthorized();
        var session = await store.FindSessionAsync(tokens.Hash(token), ct);
        if (session is null || session.ExpiresAtUtc <= clock.UtcNow)
            throw Unauthorized();
        return session.UserId;
    }
    public Task<bool> LogoutAsync(string token, CancellationToken ct = default) =>
        atomic.ExecuteAsync(async () =>
        {
            await AuthenticateAsync(token, ct);
            await store.RemoveSessionAsync(tokens.Hash(token), ct);
            return true;
        }, ct);
    private static BusinessException Unauthorized() => new(401, "invalid_session", "Нужны действующие учётные данные.");
}
