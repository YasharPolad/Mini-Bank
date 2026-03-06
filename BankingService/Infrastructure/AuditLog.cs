using System.Collections.Concurrent;
using BankingService.Domain;

namespace BankingService.Infrastructure;

internal class AuditLog
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();

    public void Append(AuditEntry entry) => _entries.Enqueue(entry);

    public IReadOnlyList<AuditEntry> GetAll() => [.. _entries];
}
