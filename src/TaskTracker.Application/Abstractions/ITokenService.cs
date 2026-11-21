namespace TaskTracker.Application.Abstractions;

public record AccessToken(string Token, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AccessToken CreateToken(UserRecord user);
}
