namespace TaskTracker.Application.Abstractions;

public interface ICurrentUser
{
    string? Email { get; }
}
