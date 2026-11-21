namespace TaskTracker.Contracts.Projects;

public record ProjectDto(
    int Id,
    string Name,
    string? Description,
    int TaskCount,
    DateTime CreatedAtUtc,
    string OwnerUserId,
    string OwnerEmail,
    ProjectPermissionsDto Permissions);

// What the signed-in user may do. The UI uses it for display, and the API enforces the same rules.
public record ProjectPermissionsDto(
    bool CanEdit,
    bool CanDelete,
    bool CanTransferOwner,
    bool CanManageMembers,
    bool CanManageTasks);
