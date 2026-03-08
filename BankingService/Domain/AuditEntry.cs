namespace BankingService.Domain;

public class AuditEntry : Entity
{
    public required Operation Operation   { get; init; }
    public required IReadOnlyList<Guid> AccountIds { get; init; }
    public required Money Amount          { get; init; }
    public required Guid IdempotencyKey   { get; init; }
    public required bool IsSuccess        { get; init; }
    public string? FailureReason          { get; init; }
}
