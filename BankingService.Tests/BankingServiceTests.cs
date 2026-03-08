using BankingService.Domain;
using Xunit;

namespace BankingService.Tests;

public class BankingServiceTests
{
    private readonly BankingService _sut = new();

    [Fact]
    public void CreateAccount_ReturnsSuccess_WithCorrectProperties()
    {
        var deposit = new Money(500m, "USD");

        var result = _sut.CreateAccount("Alice", deposit, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal("Alice", result.Value!.OwnerName);
        Assert.Equal("USD", result.Value.Currency);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public void CreateAccount_SetsInitialBalance()
    {
        var deposit = new Money(500m, "USD");
        var result = _sut.CreateAccount("Alice", deposit, Guid.NewGuid());

        var balance = _sut.GetBalance(result.Value!.Id);

        Assert.True(balance.IsSuccess);
        Assert.Equal(500m, balance.Value!.Amount);
        Assert.Equal("USD", balance.Value.Currency);
    }

    [Fact]
    public void Deposit_ReturnsSuccess_AndIncreasesBalance()
    {
        var accountId = CreateAccount(500m);

        var result = _sut.Deposit(accountId, new Money(200m, "USD"), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(700m, GetBalance(accountId));
    }

    [Fact]
    public void Withdraw_ReturnsSuccess_AndDecreasesBalance()
    {
        var accountId = CreateAccount(500m);

        var result = _sut.Withdraw(accountId, new Money(200m, "USD"), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(300m, GetBalance(accountId));
    }

    [Fact]
    public void Transfer_ReturnsSuccess_AndMovesBalance()
    {
        var fromId = CreateAccount(500m);
        var toId = CreateAccount(100m);

        var result = _sut.Transfer(fromId, toId, new Money(200m, "USD"), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(300m, GetBalance(fromId));
        Assert.Equal(300m, GetBalance(toId));
    }

    [Fact]
    public void GetBalance_ReturnsCorrectBalance()
    {
        var accountId = CreateAccount(1000m);
        _sut.Deposit(accountId, new Money(500m, "USD"), Guid.NewGuid());
        _sut.Withdraw(accountId, new Money(300m, "USD"), Guid.NewGuid());

        var balance = _sut.GetBalance(accountId);

        Assert.True(balance.IsSuccess);
        Assert.Equal(1200m, balance.Value!.Amount);
    }

    [Fact]
    public void Withdraw_Fails_WhenInsufficientFunds()
    {
        var accountId = CreateAccount(100m);

        var result = _sut.Withdraw(accountId, new Money(200m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.InsufficientFunds, result.Error);
        Assert.Equal(100m, GetBalance(accountId));
    }

    [Fact]
    public void Withdraw_Fails_WhenAmountExactlyExceedsBalance()
    {
        var accountId = CreateAccount(100m);

        var result = _sut.Withdraw(accountId, new Money(100.01m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(100m, GetBalance(accountId));
    }

    [Fact]
    public void Transfer_Fails_WhenInsufficientFunds()
    {
        var fromId = CreateAccount(100m);
        var toId = CreateAccount(100m);

        var result = _sut.Transfer(fromId, toId, new Money(200m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.InsufficientFunds, result.Error);
        Assert.Equal(100m, GetBalance(fromId));
        Assert.Equal(100m, GetBalance(toId));
    }

    [Fact]
    public void Deposit_Fails_OnCurrencyMismatch()
    {
        var accountId = CreateAccount(500m);

        var result = _sut.Deposit(accountId, new Money(100m, "EUR"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.DepositCurrencyMismatch("USD", "EUR"), result.Error);
        Assert.Equal(500m, GetBalance(accountId));
    }

    [Fact]
    public void Withdraw_Fails_OnCurrencyMismatch()
    {
        var accountId = CreateAccount(500m);

        var result = _sut.Withdraw(accountId, new Money(100m, "EUR"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.WithdrawalCurrencyMismatch("USD", "EUR"), result.Error);
        Assert.Equal(500m, GetBalance(accountId));
    }

    [Fact]
    public void Transfer_Fails_OnCurrencyMismatch()
    {
        var usdAccountId = CreateAccount(500m);
        var eurAccountId = _sut.CreateAccount("Bob", new Money(100m, "EUR"), Guid.NewGuid()).Value!.Id;

        var result = _sut.Transfer(usdAccountId, eurAccountId, new Money(100m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.TransferDestinationCurrencyMismatch("EUR", "USD"), result.Error);
        Assert.Equal(500m, GetBalance(usdAccountId));
    }

    [Fact]
    public void Transfer_Fails_WhenSameAccount()
    {
        var accountId = CreateAccount(500m);

        var result = _sut.Transfer(accountId, accountId, new Money(100m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.SameAccountTransfer, result.Error);
        Assert.Equal(500m, GetBalance(accountId));
    }

    [Fact]
    public void CreateAccount_Idempotent_ReturnsSameAccount()
    {
        var key = Guid.NewGuid();

        var first  = _sut.CreateAccount("Alice", new Money(500m, "USD"), key);
        var second = _sut.CreateAccount("Alice", new Money(500m, "USD"), key);

        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.Equal(500m, GetBalance(first.Value.Id));
    }

    [Fact]
    public void Deposit_Idempotent_OnlyCreditsOnce()
    {
        var accountId = CreateAccount(500m);
        var key = Guid.NewGuid();

        _sut.Deposit(accountId, new Money(200m, "USD"), key);
        _sut.Deposit(accountId, new Money(200m, "USD"), key);

        Assert.Equal(700m, GetBalance(accountId));
    }

    [Fact]
    public void Withdraw_Idempotent_OnlyDebitsOnce()
    {
        var accountId = CreateAccount(500m);
        var key = Guid.NewGuid();

        _sut.Withdraw(accountId, new Money(200m, "USD"), key);
        _sut.Withdraw(accountId, new Money(200m, "USD"), key);

        Assert.Equal(300m, GetBalance(accountId));
    }

    [Fact]
    public void Transfer_Idempotent_OnlyMovesOnce()
    {
        var fromId = CreateAccount(500m);
        var toId   = CreateAccount(100m);
        var key    = Guid.NewGuid();

        _sut.Transfer(fromId, toId, new Money(200m, "USD"), key);
        _sut.Transfer(fromId, toId, new Money(200m, "USD"), key);

        Assert.Equal(300m, GetBalance(fromId));
        Assert.Equal(300m, GetBalance(toId));
    }

    [Fact]
    public async Task Withdraw_Concurrent_ExactlyFiftySucceed()
    {
        var accountId = CreateAccount(50m);

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => _sut.Withdraw(accountId, new Money(1m, "USD"), Guid.NewGuid())))
            .ToArray();

        var results = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(50, results.Count(r => r.IsSuccess));
        Assert.Equal(50, results.Count(r => !r.IsSuccess));
        Assert.Equal(0m, GetBalance(accountId));
    }

    [Fact]
    public async Task Transfer_Concurrent_OpposingDirections_NoDeadlock()
    {
        var accountAId = CreateAccount(500m);
        var accountBId = CreateAccount(500m);

        var aToB = Task.Run(() => _sut.Transfer(accountAId, accountBId, new Money(100m, "USD"), Guid.NewGuid()));
        var bToA = Task.Run(() => _sut.Transfer(accountBId, accountAId, new Money(100m, "USD"), Guid.NewGuid()));

        var results = await Task.WhenAll(aToB, bToA).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(results[0].IsSuccess);
        Assert.True(results[1].IsSuccess);
        Assert.Equal(1000m, GetBalance(accountAId) + GetBalance(accountBId));
    }

    [Fact]
    public async Task Transfer_Concurrent_ManyOpposingTransfers_NoDeadlockAndBalancePreserved()
    {
        var accountAId = CreateAccount(500m);
        var accountBId = CreateAccount(500m);

        var tasks = Enumerable.Range(0, 50)
            .Select(i => i % 2 == 0
                ? Task.Run(() => _sut.Transfer(accountAId, accountBId, new Money(10m, "USD"), Guid.NewGuid()))
                : Task.Run(() => _sut.Transfer(accountBId, accountAId, new Money(10m, "USD"), Guid.NewGuid())))
            .ToArray();

        var results = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Equal(1000m, GetBalance(accountAId) + GetBalance(accountBId));
    }

    [Fact]
    public void MoneySupply_EqualsDepositsMinusWithdrawals()
    {
        var a = CreateAccount(1000m);
        var b = CreateAccount(500m);
        var c = CreateAccount(250m);

        _sut.Deposit(a, new Money(200m, "USD"), Guid.NewGuid());
        _sut.Deposit(b, new Money(100m, "USD"), Guid.NewGuid());
        _sut.Withdraw(a, new Money(300m, "USD"), Guid.NewGuid());
        _sut.Withdraw(c, new Money(50m, "USD"), Guid.NewGuid());
        _sut.Transfer(a, b, new Money(150m, "USD"), Guid.NewGuid());
        _sut.Transfer(b, c, new Money(200m, "USD"), Guid.NewGuid());

        var totalDeposited = 1000m + 500m + 250m + 200m + 100m;
        var totalWithdrawn = 300m + 50m;
        var expectedSupply = totalDeposited - totalWithdrawn;

        var actualSupply = GetBalance(a) + GetBalance(b) + GetBalance(c);

        Assert.Equal(expectedSupply, actualSupply);
    }

    // CreateAccount validation

    [Fact]
    public void CreateAccount_Fails_WhenOwnerNameIsEmpty()
    {
        var result = _sut.CreateAccount("", new Money(100m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.OwnerNameRequired, result.Error);
    }

    [Fact]
    public void CreateAccount_Fails_WhenOwnerNameIsWhitespace()
    {
        var result = _sut.CreateAccount("   ", new Money(100m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.OwnerNameRequired, result.Error);
    }

    [Fact]
    public void CreateAccount_Fails_WhenInitialDepositIsZero()
    {
        var result = _sut.CreateAccount("Alice", new Money(0m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.InitialDepositMustBePositive, result.Error);
    }

    // Account not found

    [Fact]
    public void Deposit_Fails_WhenAccountNotFound()
    {
        var result = _sut.Deposit(Guid.NewGuid(), new Money(100m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.AccountNotFound, result.Error);
    }

    [Fact]
    public void Withdraw_Fails_WhenAccountNotFound()
    {
        var result = _sut.Withdraw(Guid.NewGuid(), new Money(100m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.AccountNotFound, result.Error);
    }

    [Fact]
    public void GetBalance_Fails_WhenAccountNotFound()
    {
        var result = _sut.GetBalance(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.AccountNotFound, result.Error);
    }

    [Fact]
    public void Transfer_Fails_WhenSourceAccountNotFound()
    {
        var toId = CreateAccount(100m);

        var result = _sut.Transfer(Guid.NewGuid(), toId, new Money(100m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.SourceAccountNotFound, result.Error);
    }

    [Fact]
    public void Transfer_Fails_WhenDestinationAccountNotFound()
    {
        var fromId = CreateAccount(100m);

        var result = _sut.Transfer(fromId, Guid.NewGuid(), new Money(100m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.DestinationAccountNotFound, result.Error);
    }

    // Zero amounts

    [Fact]
    public void Deposit_Fails_WhenAmountIsZero()
    {
        var accountId = CreateAccount(100m);

        var result = _sut.Deposit(accountId, new Money(0m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.DepositAmountMustBePositive, result.Error);
    }

    [Fact]
    public void Withdraw_Fails_WhenAmountIsZero()
    {
        var accountId = CreateAccount(100m);

        var result = _sut.Withdraw(accountId, new Money(0m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.WithdrawalAmountMustBePositive, result.Error);
    }

    [Fact]
    public void Transfer_Fails_WhenAmountIsZero()
    {
        var fromId = CreateAccount(100m);
        var toId   = CreateAccount(100m);

        var result = _sut.Transfer(fromId, toId, new Money(0m, "USD"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.TransferAmountMustBePositive, result.Error);
    }

    // Money invariants

    [Fact]
    public void Money_Throws_WhenCurrencyIsNull()
    {
        Assert.Throws<ArgumentException>(() => new Money(100m, null!));
    }

    [Fact]
    public void Money_Throws_WhenCurrencyIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new Money(100m, ""));
    }

    [Fact]
    public void Money_Throws_WhenAmountIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m, "USD"));
    }

    [Fact]
    public void Money_NormalizesCurrencyToUppercase()
    {
        var money = new Money(100m, "usd");

        Assert.Equal("USD", money.Currency);
    }

    // Idempotency on failure

    [Fact]
    public void Withdraw_Idempotent_ReturnsSameFailureOnRetry()
    {
        var accountId = CreateAccount(50m);
        var key       = Guid.NewGuid();

        var first = _sut.Withdraw(accountId, new Money(100m, "USD"), key);
        _sut.Deposit(accountId, new Money(200m, "USD"), Guid.NewGuid());
        var second = _sut.Withdraw(accountId, new Money(100m, "USD"), key);

        Assert.False(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal(first.Error, second.Error);
        Assert.Equal(250m, GetBalance(accountId));
    }

    // Helpers

    private Guid CreateAccount(decimal initialDeposit)
    {
        var result = _sut.CreateAccount("Test User", new Money(initialDeposit, "USD"), Guid.NewGuid());
        return result.Value!.Id;
    }

    private decimal GetBalance(Guid accountId) => _sut.GetBalance(accountId).Value!.Amount;
}
