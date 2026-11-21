namespace TaskTracker.Contracts.Users;

public record UserDto(string Id, string Email, OrganizationRole Role, bool IsActive);

public record OwnedProjectDto(int Id, string Name);

public record CurrentUserDto(
    string Id,
    string Email,
    bool IsSuperAdmin,
    int? OrganizationId,
    string? OrganizationName,
    OrganizationRole? OrganizationRole);
