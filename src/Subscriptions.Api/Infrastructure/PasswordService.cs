using Microsoft.AspNetCore.Identity;
using Subscriptions.Business.Abstractions;
namespace Subscriptions.Api.Infrastructure;


public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<string> hasher = new();
    public string Hash(Guid userId, string password) => hasher.HashPassword(userId.ToString(), password);
    public bool Verify(Guid userId, string hash, string password) =>
        hasher.VerifyHashedPassword(userId.ToString(), hash, password) != PasswordVerificationResult.Failed;
}
