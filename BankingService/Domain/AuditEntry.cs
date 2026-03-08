namespace BankingService.Domain;

public class AuditEntry : Entity
{
    public required Operation Operation { get; init; }
    public required List<Guid> AccountIds { get; init; }
    public required Money Amount { get; init; }
    public required Guid IdempotencyKey { get; init; }
    public required string Outcome { get; init; }
}
