using System.Collections.Concurrent;

namespace BankingService.Infrastructure;

internal class IdempotencyStore
{
    private readonly ConcurrentDictionary<Guid, Lazy<object>> _store = new();

    public T ExecuteOnce<T>(Guid key, Func<T> operation) where T : class =>
        (T)_store.GetOrAdd(key, _ => new Lazy<object>(() => operation())).Value;
}
