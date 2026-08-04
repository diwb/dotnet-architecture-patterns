namespace ArchitecturePatterns.SharedKernel;

public sealed record Error(string Code, string Message, IReadOnlyDictionary<string, string[]>? Validation = null)
{
    public static Error None { get; } = new("none", string.Empty);
}

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

public sealed class Result<T> : Result
{
    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error) => Value = value;

    public T? Value { get; }
}

public static class ErrorCodes
{
    public const string Validation = "validation";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
    public const string DomainRule = "domain_rule";
}
