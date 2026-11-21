namespace TaskTracker.Application.Abstractions;

// Keeps ASP.NET Core Identity out of the Application layer.
public interface IUserAuthenticator
{
    /// <returns>The user, or null when the credentials are wrong or the account is deactivated.</returns>
    Task<UserRecord?> AuthenticateAsync(string email, string password, CancellationToken ct);
}
