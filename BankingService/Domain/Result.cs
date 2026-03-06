namespace BankingService.Domain;

public class Result
{
    public bool IsSuccess { get; private init; }
    public string Error { get; private init; } = string.Empty;

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}

public class Result<T>
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public string Error { get; private init; } = string.Empty;

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}
