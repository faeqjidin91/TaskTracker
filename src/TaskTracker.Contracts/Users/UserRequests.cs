namespace TaskTracker.Contracts.Users;

public record CreateUserRequest(string Email, string Password, UserRole Role);

public record UpdateUserRoleRequest(UserRole Role);

public record UpdateUserStatusRequest(bool IsActive);

public record ResetPasswordRequest(string NewPassword);
