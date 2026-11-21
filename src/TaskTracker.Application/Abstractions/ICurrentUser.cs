namespace TaskTracker.Application.Abstractions;

public interface ICurrentUser
{
    string? Id { get; }
    string? Email { get; }
}
