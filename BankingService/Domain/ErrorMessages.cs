namespace BankingService.Domain;

public static class ErrorMessages
{
    public const string OwnerNameRequired              = "Owner name must not be empty.";
    public const string InitialDepositMustBePositive   = "Initial deposit must be positive.";
    public const string DepositAmountMustBePositive    = "Deposit amount must be positive.";
    public const string WithdrawalAmountMustBePositive = "Withdrawal amount must be positive.";
    public const string TransferAmountMustBePositive   = "Transfer amount must be positive.";
    public const string AccountNotFound                = "Account not found.";
    public const string SourceAccountNotFound          = "Source account not found.";
    public const string DestinationAccountNotFound     = "Destination account not found.";
    public const string InsufficientFunds              = "Insufficient funds.";
    public const string SameAccountTransfer            = "Cannot transfer to the same account.";
    public const string TransferCompensated            = "Transfer failed after debit — compensated.";

    public static string DepositCurrencyMismatch(string accountCurrency, string depositCurrency) =>
        $"Currency mismatch: account is {accountCurrency}, deposit is {depositCurrency}.";

    public static string WithdrawalCurrencyMismatch(string accountCurrency, string withdrawalCurrency) =>
        $"Currency mismatch: account is {accountCurrency}, withdrawal is {withdrawalCurrency}.";

    public static string TransferSourceCurrencyMismatch(string accountCurrency, string transferCurrency) =>
        $"Currency mismatch: source account is {accountCurrency}, transfer is {transferCurrency}.";

    public static string TransferDestinationCurrencyMismatch(string accountCurrency, string transferCurrency) =>
        $"Currency mismatch: destination account is {accountCurrency}, transfer is {transferCurrency}.";
}
