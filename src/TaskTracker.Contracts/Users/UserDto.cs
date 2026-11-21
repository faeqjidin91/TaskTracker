namespace TaskTracker.Contracts.Users;

public record UserDto(string Id, string Email, UserRole Role, bool IsActive);
