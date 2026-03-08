using BankingService.Domain;

namespace BankingService.Infrastructure;

internal class Auditor
{
    private readonly AuditLog _log = new();

    public IReadOnlyList<AuditEntry> GetAll() => _log.GetAll();

    public Result Succeed(Operation operation, List<Guid> accountIds, Money amount, Guid idempotencyKey)
    {
        Append(operation, accountIds, amount, idempotencyKey, "Success");
        return Result.Success();
    }

    public Result<T> Succeed<T>(Operation operation, List<Guid> accountIds, Money amount, Guid idempotencyKey, T value)
    {
        Append(operation, accountIds, amount, idempotencyKey, "Success");
        return Result<T>.Success(value);
    }

    public Result Fail(Operation operation, List<Guid> accountIds, Money amount, Guid idempotencyKey, string error)
    {
        Append(operation, accountIds, amount, idempotencyKey, $"Failed: {error}");
        return Result.Failure(error);
    }

    public Result<T> Fail<T>(Operation operation, List<Guid> accountIds, Money amount, Guid idempotencyKey, string error)
    {
        Append(operation, accountIds, amount, idempotencyKey, $"Failed: {error}");
        return Result<T>.Failure(error);
    }

    private void Append(Operation operation, List<Guid> accountIds, Money amount, Guid idempotencyKey, string outcome)
    {
        _log.Append(new AuditEntry
        {
            Operation = operation,
            AccountIds = accountIds,
            Amount = amount,
            IdempotencyKey = idempotencyKey,
            Outcome = outcome
        });
    }
}
