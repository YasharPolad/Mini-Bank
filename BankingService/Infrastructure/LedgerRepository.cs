using System.Collections.Concurrent;
using BankingService.Domain;

namespace BankingService.Infrastructure;

internal class LedgerRepository
{
    private readonly ConcurrentDictionary<Guid, List<LedgerEntry>> _entries = new();

    public void Append(LedgerEntry entry)
    {
        _entries.GetOrAdd(entry.AccountId, _ => []).Add(entry);
    }

    public IReadOnlyList<LedgerEntry> GetByAccountId(Guid accountId) =>
        _entries.TryGetValue(accountId, out var list) ? list.AsReadOnly() : [];
}
