using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Common;
using TaskTracker.Application.Tests.Support;
using TaskTracker.Contracts.Projects;
using DomainRole = TaskTracker.Domain.Enums.ProjectRole;

namespace TaskTracker.Application.Tests;

public class ProjectMemberServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly AddProjectMemberRequest AddOutsider = new(TestUsers.Outsider, ProjectRole.Viewer);

    private static DomainRole? SavedRole(TestApp app, string userId) => app.Saved(db => db.ProjectMembers
        .IgnoreQueryFilters()
        .Where(m => m.ProjectId == TestApp.AcmeProjectId && m.UserId == userId)
        .Select(m => (DomainRole?)m.Role)
        .FirstOrDefault());

    [Fact]
    public async Task Add_ByOwner_Succeeds()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Owner);

        var result = await app.Members.AddAsync(TestApp.AcmeProjectId, AddOutsider, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(DomainRole.Viewer, SavedRole(app, TestUsers.Outsider));
    }

    [Theory]
    [InlineData(TestUsers.AcmeAdmin, ResultStatus.Forbidden)]
    [InlineData(TestUsers.Contributor, ResultStatus.Forbidden)]
    [InlineData(TestUsers.Viewer, ResultStatus.Forbidden)]
    [InlineData(TestUsers.Outsider, ResultStatus.NotFound)]
    [InlineData(TestUsers.GlobexAdmin, ResultStatus.NotFound)]
    [InlineData(TestUsers.SuperAdmin, ResultStatus.NotFound)]
    public async Task OnlyTheOwner_CanAddUpdateOrRemoveMembers(string userId, ResultStatus expected)
    {
        using var app = new TestApp();
        app.SignInAs(userId);

        var added = await app.Members.AddAsync(TestApp.AcmeProjectId, AddOutsider, Ct);
        var updated = await app.Members.UpdateAsync(
            TestApp.AcmeProjectId, TestUsers.Viewer, new UpdateProjectMemberRequest(ProjectRole.Contributor), Ct);
        var removed = await app.Members.RemoveAsync(TestApp.AcmeProjectId, TestUsers.Viewer, Ct);

        Assert.Equal(expected, added.Status);
        Assert.Equal(expected, updated.Status);
        Assert.Equal(expected, removed.Status);
        Assert.Null(SavedRole(app, TestUsers.Outsider));
        Assert.Equal(DomainRole.Viewer, SavedRole(app, TestUsers.Viewer));
    }

    [Theory]
    [InlineData(TestUsers.GlobexAdmin)]
    [InlineData("unknown-user")]
    public async Task Add_UserOutsideTheOrganization_IsInvalid(string targetId)
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Owner);

        var result = await app.Members.AddAsync(
            TestApp.AcmeProjectId, new AddProjectMemberRequest(targetId, ProjectRole.Viewer), Ct);

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
    }

    [Fact]
    public async Task Add_InactiveUser_IsInvalid()
    {
        using var app = new TestApp();
        app.Users.Update(TestUsers.Outsider, user => user with { IsActive = false });
        app.SignInAs(TestUsers.Owner);

        var result = await app.Members.AddAsync(TestApp.AcmeProjectId, AddOutsider, Ct);

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
    }

    [Theory]
    [InlineData(TestUsers.Owner)]
    [InlineData(TestUsers.Contributor)]
    public async Task Add_OwnerOrExistingMember_IsConflict(string targetId)
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Owner);

        var result = await app.Members.AddAsync(
            TestApp.AcmeProjectId, new AddProjectMemberRequest(targetId, ProjectRole.Viewer), Ct);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task UpdateAndRemove_ByOwner_Succeed()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Owner);

        var updated = await app.Members.UpdateAsync(
            TestApp.AcmeProjectId, TestUsers.Viewer, new UpdateProjectMemberRequest(ProjectRole.Contributor), Ct);
        Assert.True(updated.IsSuccess);
        Assert.Equal(DomainRole.Contributor, SavedRole(app, TestUsers.Viewer));

        var removed = await app.Members.RemoveAsync(TestApp.AcmeProjectId, TestUsers.Viewer, Ct);
        Assert.True(removed.IsSuccess);
        Assert.Null(SavedRole(app, TestUsers.Viewer));
    }

    [Fact]
    public async Task GetMembers_ExcludesTheOwner()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Viewer);

        var result = await app.Members.GetMembersAsync(TestApp.AcmeProjectId, Ct);

        Assert.Equal(2, result.Value.Count);
        Assert.DoesNotContain(result.Value, member => member.UserId == TestUsers.Owner);
    }

    [Fact]
    public async Task GetCandidates_ReturnsOnlyActiveUsersInTheOrganization()
    {
        using var app = new TestApp();
        app.Users.Update(TestUsers.Outsider, user => user with { IsActive = false });
        app.SignInAs(TestUsers.Owner);

        var result = await app.Members.GetCandidatesAsync(TestApp.AcmeProjectId, Ct);

        Assert.DoesNotContain(result.Value, candidate => candidate.UserId == TestUsers.Outsider);
        Assert.DoesNotContain(result.Value, candidate => candidate.UserId == TestUsers.GlobexAdmin);
    }

    [Fact]
    public async Task GetCandidates_ByContributor_IsForbidden()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Contributor);

        var result = await app.Members.GetCandidatesAsync(TestApp.AcmeProjectId, Ct);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Theory]
    [InlineData(TestUsers.Owner)]
    [InlineData(TestUsers.AcmeAdmin)]
    public async Task TransferOwner_ByOwnerOrAdmin_SwapsOwnerAndKeepsPreviousOwnerAsContributor(string userId)
    {
        using var app = new TestApp();
        app.SignInAs(userId);

        var result = await app.Members.TransferOwnerAsync(
            TestApp.AcmeProjectId, new TransferProjectOwnerRequest(TestUsers.Contributor), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestUsers.Contributor, app.SavedProject(TestApp.AcmeProjectId).OwnerUserId);
        Assert.Null(SavedRole(app, TestUsers.Contributor));
        Assert.Equal(DomainRole.Contributor, SavedRole(app, TestUsers.Owner));
    }

    [Theory]
    [InlineData(TestUsers.Contributor, ResultStatus.Forbidden)]
    [InlineData(TestUsers.Viewer, ResultStatus.Forbidden)]
    [InlineData(TestUsers.Outsider, ResultStatus.NotFound)]
    [InlineData(TestUsers.GlobexAdmin, ResultStatus.NotFound)]
    public async Task TransferOwner_ByOthers_IsDenied(string userId, ResultStatus expected)
    {
        using var app = new TestApp();
        app.SignInAs(userId);

        var result = await app.Members.TransferOwnerAsync(
            TestApp.AcmeProjectId, new TransferProjectOwnerRequest(TestUsers.Contributor), Ct);

        Assert.Equal(expected, result.Status);
        Assert.Equal(TestUsers.Owner, app.SavedProject(TestApp.AcmeProjectId).OwnerUserId);
    }

    [Fact]
    public async Task TransferOwner_ToUserInAnotherOrganization_IsInvalid()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Owner);

        var result = await app.Members.TransferOwnerAsync(
            TestApp.AcmeProjectId, new TransferProjectOwnerRequest(TestUsers.GlobexAdmin), Ct);

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
        Assert.Equal(TestUsers.Owner, app.SavedProject(TestApp.AcmeProjectId).OwnerUserId);
    }

    [Fact]
    public async Task TransferOwner_ToInactiveUser_IsInvalid()
    {
        using var app = new TestApp();
        app.Users.Update(TestUsers.Contributor, user => user with { IsActive = false });
        app.SignInAs(TestUsers.Owner);

        var result = await app.Members.TransferOwnerAsync(
            TestApp.AcmeProjectId, new TransferProjectOwnerRequest(TestUsers.Contributor), Ct);

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
    }
}
