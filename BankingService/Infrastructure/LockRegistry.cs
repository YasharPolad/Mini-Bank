using System.Collections.Concurrent;

namespace BankingService.Infrastructure;

internal class LockRegistry
{
    private readonly ConcurrentDictionary<Guid, object> _locks = new();

    public object GetOrCreate(Guid accountId) => _locks.GetOrAdd(accountId, _ => new object());
}
