namespace BankingService.Domain;

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must not be negative.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must not be empty.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
}
