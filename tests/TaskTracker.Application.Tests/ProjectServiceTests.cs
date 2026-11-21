using TaskTracker.Application.Common;
using TaskTracker.Application.Tests.Support;
using TaskTracker.Contracts.Projects;

namespace TaskTracker.Application.Tests;

public class ProjectServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task Create_WithoutOwner_MakesTheAdminTheOwner()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.Projects.CreateAsync(new CreateProjectRequest("  New  ", null), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", result.Value.Name);
        Assert.Equal(TestUsers.AcmeAdmin, result.Value.OwnerUserId);
        Assert.True(result.Value.Permissions.CanManageMembers);
        Assert.Equal(TestApp.AcmeId, app.SavedProject(result.Value.Id).OrganizationId);
    }

    [Fact]
    public async Task Create_WithOwner_AssignsThatOwner()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.Projects.CreateAsync(new CreateProjectRequest("New", null, TestUsers.Outsider), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestUsers.Outsider, result.Value.OwnerUserId);
        Assert.False(result.Value.Permissions.CanManageMembers);
    }

    [Fact]
    public async Task Create_ByNonAdmin_IsForbidden()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Owner);

        var result = await app.Projects.CreateAsync(new CreateProjectRequest("New", null), Ct);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Theory]
    [InlineData(TestUsers.GlobexAdmin)]
    [InlineData("unknown-user")]
    public async Task Create_WithOwnerOutsideTheOrganization_IsInvalid(string ownerId)
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.Projects.CreateAsync(new CreateProjectRequest("New", null, ownerId), Ct);

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
        Assert.Contains(nameof(CreateProjectRequest.OwnerUserId), result.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithInactiveOwner_IsInvalid()
    {
        using var app = new TestApp();
        app.Users.Update(TestUsers.Outsider, user => user with { IsActive = false });
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.Projects.CreateAsync(new CreateProjectRequest("New", null, TestUsers.Outsider), Ct);

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
    }

    [Theory]
    [InlineData(TestUsers.AcmeAdmin, 1)]
    [InlineData(TestUsers.Owner, 1)]
    [InlineData(TestUsers.Contributor, 1)]
    [InlineData(TestUsers.Viewer, 1)]
    [InlineData(TestUsers.Outsider, 0)]
    public async Task GetAll_ReturnsOnlyVisibleProjects(string userId, int expected)
    {
        using var app = new TestApp();
        app.SignInAs(userId);

        var result = await app.Projects.GetAllAsync(Ct);

        Assert.Equal(expected, result.Value.Count);
    }

    [Theory]
    [InlineData(TestUsers.AcmeAdmin)]
    [InlineData(TestUsers.Owner)]
    public async Task Update_ByAdminOrOwner_Succeeds(string userId)
    {
        using var app = new TestApp();
        app.SignInAs(userId);

        var result = await app.Projects.UpdateAsync(TestApp.AcmeProjectId, new UpdateProjectRequest("Renamed", null), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", app.SavedProject(TestApp.AcmeProjectId).Name);
    }

    [Theory]
    [InlineData(TestUsers.Contributor, ResultStatus.Forbidden)]
    [InlineData(TestUsers.Viewer, ResultStatus.Forbidden)]
    [InlineData(TestUsers.Outsider, ResultStatus.NotFound)]
    public async Task Update_ByOthers_IsDenied(string userId, ResultStatus expected)
    {
        using var app = new TestApp();
        app.SignInAs(userId);

        var result = await app.Projects.UpdateAsync(TestApp.AcmeProjectId, new UpdateProjectRequest("Renamed", null), Ct);

        Assert.Equal(expected, result.Status);
        Assert.Equal("Rocket", app.SavedProject(TestApp.AcmeProjectId).Name);
    }

    [Theory]
    [InlineData(TestUsers.Owner, ResultStatus.Forbidden)]
    [InlineData(TestUsers.Contributor, ResultStatus.Forbidden)]
    [InlineData(TestUsers.Outsider, ResultStatus.NotFound)]
    public async Task Delete_ByNonAdmin_IsDenied(string userId, ResultStatus expected)
    {
        using var app = new TestApp();
        app.SignInAs(userId);

        var result = await app.Projects.DeleteAsync(TestApp.AcmeProjectId, Ct);

        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public async Task Delete_ByAdmin_Succeeds()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.Projects.DeleteAsync(TestApp.AcmeProjectId, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, (await app.Projects.GetByIdAsync(TestApp.AcmeProjectId, Ct)).Status);
    }
}
