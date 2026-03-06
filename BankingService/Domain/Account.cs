namespace BankingService.Domain;

public class Account : Entity
{
    public required string OwnerName { get; init; }
    public required string Currency { get; init; }
}
