using System.Text.Json.Serialization;

namespace TaskTracker.Contracts.Organizations;

public record OrganizationDto(int Id, string Name, bool IsActive, int UserCount, DateTime CreatedAtUtc);

// An organization is always created with its first Admin, so it is never left unmanaged.
public record CreateOrganizationRequest(string Name, string AdminEmail, string AdminPassword);

// JsonRequired: without it, an empty body would bind to false and deactivate the organization.
public record UpdateOrganizationStatusRequest([property: JsonRequired] bool IsActive);

public record AddOrganizationAdminRequest(string Email, string Password);
