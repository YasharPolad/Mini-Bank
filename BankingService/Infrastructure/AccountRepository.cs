using System.Collections.Concurrent;
using BankingService.Domain;

namespace BankingService.Infrastructure;

internal class AccountRepository
{
    private readonly ConcurrentDictionary<Guid, Account> _accounts = new();

    public void Add(Account account) => _accounts[account.Id] = account;

    public Account? GetById(Guid id) => _accounts.GetValueOrDefault(id);
}
