using TaskTracker.Application.Common;
using TaskTracker.Application.Tests.Support;
using TaskTracker.Contracts.Projects;
using TaskTracker.Contracts.Tasks;
using TaskTracker.Contracts.Users;
using DomainRole = TaskTracker.Domain.Enums.OrganizationRole;
using DomainStatus = TaskTracker.Domain.Enums.TaskItemStatus;

namespace TaskTracker.Application.Tests;

// These run behind TestDbContext's mirrored filters. See the note on that class.
public class TenantIsolationTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task Admin_CannotSeeAnotherOrganizationsProjects()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var all = await app.Projects.GetAllAsync(Ct);
        var other = await app.Projects.GetByIdAsync(TestApp.GlobexProjectId, Ct);

        Assert.DoesNotContain(all.Value, project => project.Id == TestApp.GlobexProjectId);
        Assert.Equal(ResultStatus.NotFound, other.Status);
    }

    [Fact]
    public async Task Admin_CannotChangeAnotherOrganizationsProject()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var updated = await app.Projects.UpdateAsync(TestApp.GlobexProjectId, new UpdateProjectRequest("Hacked", null), Ct);
        var deleted = await app.Projects.DeleteAsync(TestApp.GlobexProjectId, Ct);
        var transferred = await app.Members.TransferOwnerAsync(
            TestApp.GlobexProjectId, new TransferProjectOwnerRequest(TestUsers.AcmeAdmin), Ct);

        Assert.Equal(ResultStatus.NotFound, updated.Status);
        Assert.Equal(ResultStatus.NotFound, deleted.Status);
        Assert.Equal(ResultStatus.NotFound, transferred.Status);
        var saved = app.SavedProject(TestApp.GlobexProjectId);
        Assert.Equal("Secret", saved.Name);
        Assert.Equal(TestUsers.GlobexAdmin, saved.OwnerUserId);
    }

    [Fact]
    public async Task User_CannotReachAnotherOrganizationsTasks()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Owner);
        var edit = new UpdateTaskRequest("Hacked", null, TaskPriority.High, null);

        Assert.Equal(ResultStatus.NotFound, (await app.Tasks.GetByProjectAsync(TestApp.GlobexProjectId, Ct)).Status);
        Assert.Equal(ResultStatus.NotFound, (await app.Tasks.GetByIdAsync(TestApp.GlobexTaskId, Ct)).Status);
        Assert.Equal(ResultStatus.NotFound, (await app.Tasks.UpdateAsync(TestApp.GlobexTaskId, edit, Ct)).Status);
        Assert.Equal(ResultStatus.NotFound, (await app.Tasks.UpdateStatusAsync(
            TestApp.GlobexTaskId, new UpdateTaskStatusRequest(TaskItemStatus.Done), Ct)).Status);
        Assert.Equal(ResultStatus.NotFound, (await app.Tasks.DeleteAsync(TestApp.GlobexTaskId, Ct)).Status);

        var saved = app.SavedTask(TestApp.GlobexTaskId);
        Assert.Equal("Hidden", saved.Title);
        Assert.Equal(DomainStatus.Todo, saved.Status);
        Assert.Empty(app.Publisher.Events);
    }

    [Fact]
    public async Task Admin_CannotManageAnotherOrganizationsUsers()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        Assert.Equal(ResultStatus.NotFound, (await app.UserAdmin.GetByIdAsync(TestUsers.GlobexAdmin, Ct)).Status);
        Assert.Equal(ResultStatus.NotFound, (await app.UserAdmin.UpdateRoleAsync(
            TestUsers.GlobexAdmin, new UpdateUserRoleRequest(OrganizationRole.Member), Ct)).Status);
        Assert.Equal(ResultStatus.NotFound, (await app.UserAdmin.UpdateStatusAsync(
            TestUsers.GlobexAdmin, new UpdateUserStatusRequest(false), Ct)).Status);
        Assert.Equal(ResultStatus.NotFound, (await app.UserAdmin.ResetPasswordAsync(
            TestUsers.GlobexAdmin, new ResetPasswordRequest("N3w!Pass"), Ct)).Status);

        var target = app.Users.Get(TestUsers.GlobexAdmin);
        Assert.Equal(DomainRole.Admin, target.OrganizationRole);
        Assert.True(target.IsActive);
    }

    [Fact]
    public async Task SuperAdmin_CannotReadProjectOrTaskData()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.SuperAdmin);

        Assert.Empty((await app.Projects.GetAllAsync(Ct)).Value);
        Assert.Equal(ResultStatus.NotFound, (await app.Projects.GetByIdAsync(TestApp.AcmeProjectId, Ct)).Status);
        Assert.Equal(ResultStatus.NotFound, (await app.Tasks.GetByIdAsync(TestApp.AcmeTaskId, Ct)).Status);
        Assert.Equal(ResultStatus.NotFound, (await app.Members.GetMembersAsync(TestApp.AcmeProjectId, Ct)).Status);
    }

    [Fact]
    public async Task NewProject_IsCreatedInTheCallersOrganization()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.GlobexAdmin);

        var created = await app.Projects.CreateAsync(new CreateProjectRequest("Globex only", null), Ct);

        Assert.Equal(TestApp.GlobexId, app.SavedProject(created.Value.Id).OrganizationId);

        app.SignInAs(TestUsers.AcmeAdmin);
        Assert.Equal(ResultStatus.NotFound, (await app.Projects.GetByIdAsync(created.Value.Id, Ct)).Status);
    }
}
