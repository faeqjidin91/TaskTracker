namespace TaskTracker.Application.Abstractions;

public record AuthenticatedUser(string Id, string Email);

// Keeps ASP.NET Core Identity out of the Application layer.
public interface IUserAuthenticator
{
    /// <returns>The user, or null when the email or password is wrong.</returns>
    Task<AuthenticatedUser?> AuthenticateAsync(string email, string password, CancellationToken ct);
}
