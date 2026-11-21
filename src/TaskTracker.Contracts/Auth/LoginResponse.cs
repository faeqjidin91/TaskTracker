namespace TaskTracker.Contracts.Auth;

public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string Email);
