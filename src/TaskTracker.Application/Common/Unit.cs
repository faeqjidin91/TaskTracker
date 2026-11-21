namespace TaskTracker.Application.Common;

// The Result<T> value for operations that return nothing on success, such as Delete.
public readonly record struct Unit
{
    public static Unit Value => default;
}
