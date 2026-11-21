using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Authorization;

// The single definition of who can do what inside a project.
public sealed record ProjectPermissions(
    bool CanEdit,
    bool CanDelete,
    bool CanTransferOwner,
    bool CanManageMembers,
    bool CanManageTasks)
{
    /// <returns>The permissions, or null when the user cannot see the project at all.</returns>
    public static ProjectPermissions? For(bool isOrganizationAdmin, bool isOwner, ProjectRole? memberRole)
    {
        if (!isOrganizationAdmin && !isOwner && memberRole is null)
        {
            return null;
        }

        return new ProjectPermissions(
            CanEdit: isOrganizationAdmin || isOwner,
            CanDelete: isOrganizationAdmin,
            // Admins can transfer so a project is never stranded when its owner leaves.
            CanTransferOwner: isOrganizationAdmin || isOwner,
            CanManageMembers: isOwner,
            CanManageTasks: isOwner || memberRole == ProjectRole.Contributor);
    }
}
