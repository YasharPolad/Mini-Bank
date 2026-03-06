namespace BankingService.Domain;

public class LedgerEntry : Entity
{
    public required Guid AccountId { get; init; }
    public required Money Amount { get; init; }
    public required EntryType Type { get; init; }
    public required Guid IdempotencyKey { get; init; }
    public Guid? RelatedAccountId { get; init; }
}
