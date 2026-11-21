namespace TaskTracker.Contracts.Users;

public record CreateUserRequest(string Email, string Password, OrganizationRole Role);

public record UpdateUserRoleRequest(OrganizationRole Role);

public record UpdateUserStatusRequest(bool IsActive);

public record ResetPasswordRequest(string NewPassword);
