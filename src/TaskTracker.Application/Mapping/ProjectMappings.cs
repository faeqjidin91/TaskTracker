using TaskTracker.Application.Authorization;
using TaskTracker.Application.Common;
using TaskTracker.Contracts.Projects;
using TaskTracker.Domain.Entities;
using ContractRole = TaskTracker.Contracts.Projects.ProjectRole;
using DomainRole = TaskTracker.Domain.Enums.ProjectRole;

namespace TaskTracker.Application.Mapping;

public static class ProjectMappings
{
    // The count is passed in so callers can get it from SQL without loading every task.
    public static ProjectDto ToDto(
        this Project project, int taskCount, string ownerEmail, ProjectPermissions permissions) => new(
        project.Id,
        project.Name,
        project.Description,
        taskCount,
        project.CreatedAtUtc.AsUtc(),
        project.OwnerUserId,
        ownerEmail,
        new ProjectPermissionsDto(
            permissions.CanEdit,
            permissions.CanDelete,
            permissions.CanTransferOwner,
            permissions.CanManageMembers,
            permissions.CanManageTasks));

    public static ContractRole ToContract(this DomainRole role) => role switch
    {
        DomainRole.Viewer => ContractRole.Viewer,
        DomainRole.Contributor => ContractRole.Contributor,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.")
    };

    public static DomainRole ToDomain(this ContractRole role) => role switch
    {
        ContractRole.Viewer => DomainRole.Viewer,
        ContractRole.Contributor => DomainRole.Contributor,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.")
    };
}
