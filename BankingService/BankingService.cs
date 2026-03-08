using BankingService.Domain;
using BankingService.Infrastructure;

namespace BankingService;

public class BankingService
{
    private readonly AccountRepository _accounts = new();
    private readonly LedgerRepository _ledger = new();
    private readonly Auditor _auditor = new();
    private readonly IdempotencyStore _idempotency = new();
    private readonly LockRegistry _locks = new();

    public Result<Account> CreateAccount(string ownerName, Money initialDeposit, Guid idempotencyKey)
    {
        return _idempotency.ExecuteOnce(idempotencyKey, () =>
        {
            if (string.IsNullOrWhiteSpace(ownerName))
                return _auditor.Fail<Account>(Operation.CreateAccount, [], initialDeposit, idempotencyKey, ErrorMessages.OwnerNameRequired);

            if (initialDeposit.Amount <= 0)
                return _auditor.Fail<Account>(Operation.CreateAccount, [], initialDeposit, idempotencyKey, ErrorMessages.InitialDepositMustBePositive);

            var account = new Account { OwnerName = ownerName, Currency = initialDeposit.Currency };
            var entry = new LedgerEntry { AccountId = account.Id, Amount = initialDeposit, Type = EntryType.Credit, IdempotencyKey = idempotencyKey };

            lock (_locks.GetOrCreate(account.Id))
            {
                _accounts.Add(account);
                _ledger.Append(entry);
            }

            return _auditor.Succeed(Operation.CreateAccount, [account.Id], initialDeposit, idempotencyKey, account);
        });
    }

    public Result<LedgerEntry> Deposit(Guid accountId, Money amount, Guid idempotencyKey)
    {
        return _idempotency.ExecuteOnce(idempotencyKey, () =>
        {
            if (amount.Amount <= 0)
                return _auditor.Fail<LedgerEntry>(Operation.Deposit, [accountId], amount, idempotencyKey, ErrorMessages.DepositAmountMustBePositive);

            var account = _accounts.GetById(accountId);
            if (account is null)
                return _auditor.Fail<LedgerEntry>(Operation.Deposit, [accountId], amount, idempotencyKey, ErrorMessages.AccountNotFound);

            if (amount.Currency != account.Currency)
                return _auditor.Fail<LedgerEntry>(Operation.Deposit, [accountId], amount, idempotencyKey, ErrorMessages.DepositCurrencyMismatch(account.Currency, amount.Currency));

            lock (_locks.GetOrCreate(accountId))
            {
                var entry = new LedgerEntry { AccountId = accountId, Amount = amount, Type = EntryType.Credit, IdempotencyKey = idempotencyKey };
                _ledger.Append(entry);
                return _auditor.Succeed(Operation.Deposit, [accountId], amount, idempotencyKey, entry);
            }
        });
    }

    public Result<LedgerEntry> Withdraw(Guid accountId, Money amount, Guid idempotencyKey)
    {
        return _idempotency.ExecuteOnce(idempotencyKey, () =>
        {
            if (amount.Amount <= 0)
                return _auditor.Fail<LedgerEntry>(Operation.Withdraw, [accountId], amount, idempotencyKey, ErrorMessages.WithdrawalAmountMustBePositive);

            var account = _accounts.GetById(accountId);
            if (account is null)
                return _auditor.Fail<LedgerEntry>(Operation.Withdraw, [accountId], amount, idempotencyKey, ErrorMessages.AccountNotFound);

            if (amount.Currency != account.Currency)
                return _auditor.Fail<LedgerEntry>(Operation.Withdraw, [accountId], amount, idempotencyKey, ErrorMessages.WithdrawalCurrencyMismatch(account.Currency, amount.Currency));

            lock (_locks.GetOrCreate(accountId))
            {
                var balance = ComputeBalance(accountId);
                if (balance < amount.Amount)
                    return _auditor.Fail<LedgerEntry>(Operation.Withdraw, [accountId], amount, idempotencyKey, ErrorMessages.InsufficientFunds);

                var entry = new LedgerEntry { AccountId = accountId, Amount = amount, Type = EntryType.Debit, IdempotencyKey = idempotencyKey };
                _ledger.Append(entry);
                return _auditor.Succeed(Operation.Withdraw, [accountId], amount, idempotencyKey, entry);
            }
        });
    }

    public Result Transfer(Guid fromAccountId, Guid toAccountId, Money amount, Guid idempotencyKey)
    {
        return _idempotency.ExecuteOnce(idempotencyKey, () =>
        {
            if (amount.Amount <= 0)
                return _auditor.Fail(Operation.Transfer, [fromAccountId, toAccountId], amount, idempotencyKey, ErrorMessages.TransferAmountMustBePositive);

            if (fromAccountId == toAccountId)
                return _auditor.Fail(Operation.Transfer, [fromAccountId], amount, idempotencyKey, ErrorMessages.SameAccountTransfer);

            var from = _accounts.GetById(fromAccountId);
            if (from is null)
                return _auditor.Fail(Operation.Transfer, [fromAccountId, toAccountId], amount, idempotencyKey, ErrorMessages.SourceAccountNotFound);

            var to = _accounts.GetById(toAccountId);
            if (to is null)
                return _auditor.Fail(Operation.Transfer, [fromAccountId, toAccountId], amount, idempotencyKey, ErrorMessages.DestinationAccountNotFound);

            if (amount.Currency != from.Currency)
                return _auditor.Fail(Operation.Transfer, [fromAccountId, toAccountId], amount, idempotencyKey, ErrorMessages.TransferSourceCurrencyMismatch(from.Currency, amount.Currency));

            if (amount.Currency != to.Currency)
                return _auditor.Fail(Operation.Transfer, [fromAccountId, toAccountId], amount, idempotencyKey, ErrorMessages.TransferDestinationCurrencyMismatch(to.Currency, amount.Currency));

            // Acquire locks in consistent order to prevent deadlock
            var firstId  = fromAccountId < toAccountId ? fromAccountId : toAccountId;
            var secondId = fromAccountId < toAccountId ? toAccountId   : fromAccountId;

            lock (_locks.GetOrCreate(firstId))
            lock (_locks.GetOrCreate(secondId))
            {
                var balance = ComputeBalance(fromAccountId);
                if (balance < amount.Amount)
                    return _auditor.Fail(Operation.Transfer, [fromAccountId, toAccountId], amount, idempotencyKey, ErrorMessages.InsufficientFunds);

                var debit = new LedgerEntry { AccountId = fromAccountId, Amount = amount, Type = EntryType.Debit, IdempotencyKey = idempotencyKey, RelatedAccountId = toAccountId };
                _ledger.Append(debit);

                try
                {
                    var credit = new LedgerEntry { AccountId = toAccountId, Amount = amount, Type = EntryType.Credit, IdempotencyKey = idempotencyKey, RelatedAccountId = fromAccountId };
                    _ledger.Append(credit);
                }
                catch
                {
                    // Compensate the debit so the ledger stays balanced
                    var compensation = new LedgerEntry { AccountId = fromAccountId, Amount = amount, Type = EntryType.Credit, IdempotencyKey = Guid.NewGuid() };
                    _ledger.Append(compensation);
                    return _auditor.Fail(Operation.Transfer, [fromAccountId, toAccountId], amount, idempotencyKey, ErrorMessages.TransferCompensated);
                }

                return _auditor.Succeed(Operation.Transfer, [fromAccountId, toAccountId], amount, idempotencyKey);
            }
        });
    }

    public Result<Money> GetBalance(Guid accountId)
    {
        var account = _accounts.GetById(accountId);
        if (account is null)
            return Result<Money>.Failure(ErrorMessages.AccountNotFound);

        lock (_locks.GetOrCreate(accountId))
        {
            var balance = ComputeBalance(accountId);
            return Result<Money>.Success(new Money(balance, account.Currency));
        }
    }

    private decimal ComputeBalance(Guid accountId) =>
        _ledger.GetByAccountId(accountId)
               .Sum(e => e.Type == EntryType.Credit ? e.Amount.Amount : -e.Amount.Amount);
}
