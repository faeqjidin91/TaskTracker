using System.Collections.ObjectModel;

namespace TaskTracker.Application.Common;

// Expected outcomes are returned rather than thrown. Exceptions stay for real failures.
public sealed class Result<T>
{
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors =
        new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>());

    private readonly T? _value;

    private Result(ResultStatus status, T? value, IReadOnlyDictionary<string, string[]> errors, string? message = null)
    {
        Status = status;
        _value = value;
        Errors = errors;
        Message = message;
    }

    public ResultStatus Status { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    public string? Message { get; }
    public bool IsSuccess => Status == ResultStatus.Success;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read Value of a {Status} result.");

    public static Result<T> Success(T value) => new(ResultStatus.Success, value, NoErrors);

    public static Result<T> NotFound() => new(ResultStatus.NotFound, default, NoErrors);

    public static Result<T> Unauthorized() => new(ResultStatus.Unauthorized, default, NoErrors);

    public static Result<T> Conflict(string message) => new(ResultStatus.Conflict, default, NoErrors, message);

    public static Result<T> ValidationFailed(IDictionary<string, string[]> errors) =>
        new(ResultStatus.ValidationFailed, default, new ReadOnlyDictionary<string, string[]>(errors));

    // Carries a failure across to a result of another value type.
    public Result<TOther> ToFailure<TOther>() => IsSuccess
        ? throw new InvalidOperationException("Cannot convert a successful result to a failure.")
        : new Result<TOther>(Status, default, Errors, Message);
}
