using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Tests;

public class ProjectPermissionsTests
{
    [Theory]
    //          admin  owner  member role              edit   delete transfer members tasks
    [InlineData(true,  false, null,                    true,  true,  true,    false,  false)]
    [InlineData(false, true,  null,                    true,  false, true,    true,   true)]
    [InlineData(true,  true,  null,                    true,  true,  true,    true,   true)]
    [InlineData(false, false, ProjectRole.Contributor, false, false, false,   false,  true)]
    [InlineData(false, false, ProjectRole.Viewer,      false, false, false,   false,  false)]
    [InlineData(true,  false, ProjectRole.Contributor, true,  true,  true,    false,  true)]
    [InlineData(true,  false, ProjectRole.Viewer,      true,  true,  true,    false,  false)]
    public void For_GrantsTheExpectedPermissions(
        bool isAdmin, bool isOwner, ProjectRole? role,
        bool canEdit, bool canDelete, bool canTransfer, bool canManageMembers, bool canManageTasks)
    {
        var permissions = ProjectPermissions.For(isAdmin, isOwner, role);

        Assert.NotNull(permissions);
        Assert.Equal(canEdit, permissions.CanEdit);
        Assert.Equal(canDelete, permissions.CanDelete);
        Assert.Equal(canTransfer, permissions.CanTransferOwner);
        Assert.Equal(canManageMembers, permissions.CanManageMembers);
        Assert.Equal(canManageTasks, permissions.CanManageTasks);
    }

    [Fact]
    public void For_UserWithNoRelationToTheProject_ReturnsNull()
    {
        Assert.Null(ProjectPermissions.For(isOrganizationAdmin: false, isOwner: false, memberRole: null));
    }

    [Theory]
    [InlineData(true, false, null)]
    [InlineData(false, false, ProjectRole.Contributor)]
    [InlineData(false, false, ProjectRole.Viewer)]
    [InlineData(true, false, ProjectRole.Contributor)]
    public void For_OnlyTheOwnerCanManageMembers(bool isAdmin, bool isOwner, ProjectRole? role)
    {
        Assert.False(ProjectPermissions.For(isAdmin, isOwner, role)!.CanManageMembers);
    }
}
