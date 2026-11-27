using Subscriptions.Business.Models;
namespace Subscriptions.Business.Abstractions;

public interface IPasswordService
{
    string Hash(Guid userId, string password);
    bool Verify(Guid userId, string hash, string password);
}
