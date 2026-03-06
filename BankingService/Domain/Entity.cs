namespace BankingService.Domain;

public abstract class Entity
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
}
