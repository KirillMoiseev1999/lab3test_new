using Subscriptions.Business.Models;
namespace Subscriptions.Business.Abstractions;

public interface IClock { DateTimeOffset UtcNow { get; } }
