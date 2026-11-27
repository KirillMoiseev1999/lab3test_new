using Subscriptions.Business.Models;
namespace Subscriptions.Business.Abstractions;

public interface ITokenService
{
    string Create();
    string Hash(string token);
}
