namespace TaskTracker.Web.Services;

public class ApiResult
{
    protected ApiResult(string? error) => Error = error;

    public string? Error { get; }
    public bool IsSuccess => Error is null;

    public static ApiResult Succeeded() => new(null);
    public static ApiResult Failed(string error) => new(error);
}

public sealed class ApiResult<T> : ApiResult
{
    private readonly T? _value;

    private ApiResult(T? value, string? error) : base(error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read Value of a failed result.");

    public static ApiResult<T> FromValue(T value) => new(value, null);
    public static ApiResult<T> FromError(string error) => new(default, error);
}
