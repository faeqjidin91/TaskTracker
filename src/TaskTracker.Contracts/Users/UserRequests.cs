using System.Text.Json.Serialization;

namespace TaskTracker.Contracts.Users;

// JsonRequired on value types: a missing field would otherwise bind to its default, so an
// empty body could silently demote or deactivate someone.
public record CreateUserRequest(string Email, string Password, [property: JsonRequired] OrganizationRole Role);

public record UpdateUserRoleRequest([property: JsonRequired] OrganizationRole Role);

public record UpdateUserStatusRequest([property: JsonRequired] bool IsActive);

public record ResetPasswordRequest(string NewPassword);
