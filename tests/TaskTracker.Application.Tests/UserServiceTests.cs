using TaskTracker.Application.Common;
using TaskTracker.Application.Tests.Support;
using TaskTracker.Contracts.Users;
using DomainRole = TaskTracker.Domain.Enums.OrganizationRole;

namespace TaskTracker.Application.Tests;

public class UserServiceTests
{
    private const string SecondAdmin = "acme-admin-2";
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly UpdateUserRoleRequest ToMember = new(OrganizationRole.Member);
    private static readonly UpdateUserStatusRequest Deactivate = new(false);

    [Fact]
    public async Task GetAll_ReturnsOnlyTheCallersOrganization()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.UserAdmin.GetAllAsync(Ct);

        Assert.Equal(5, result.Value.Count);
        Assert.DoesNotContain(result.Value, user => user.Id == TestUsers.GlobexAdmin);
    }

    [Fact]
    public async Task Create_AddsUserToTheCallersOrganization()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.UserAdmin.CreateAsync(
            new CreateUserRequest(" new@test.local ", "Str0ng!Pass", OrganizationRole.Member), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("new@test.local", result.Value.Email);
        Assert.Equal(TestApp.AcmeId, app.Users.Get(result.Value.Id).OrganizationId);
    }

    [Theory]
    [InlineData("not-an-email", "Str0ng!Pass", OrganizationRole.Member)]
    [InlineData("new@test.local", "", OrganizationRole.Member)]
    [InlineData("new@test.local", "Str0ng!Pass", (OrganizationRole)99)]
    public async Task Create_WithInvalidRequest_ReturnsValidationFailed(string email, string password, OrganizationRole role)
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.UserAdmin.CreateAsync(new CreateUserRequest(email, password, role), Ct);

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
    }

    [Fact]
    public async Task UpdateRole_OfOwnAccount_IsConflict()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.UserAdmin.UpdateRoleAsync(TestUsers.AcmeAdmin, ToMember, Ct);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task UpdateRole_OfOwnAccountWithDifferentIdCasing_IsConflict()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.UserAdmin.UpdateRoleAsync(TestUsers.AcmeAdmin.ToUpperInvariant(), ToMember, Ct);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(DomainRole.Admin, app.Users.Get(TestUsers.AcmeAdmin).OrganizationRole);
    }

    [Fact]
    public async Task UpdateRole_OfAnotherAdmin_Succeeds()
    {
        using var app = new TestApp();
        app.Users.Add(SecondAdmin, TestApp.AcmeId, DomainRole.Admin);
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.UserAdmin.UpdateRoleAsync(SecondAdmin, ToMember, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(DomainRole.Member, app.Users.Get(SecondAdmin).OrganizationRole);
    }

    [Fact]
    public async Task UpdateRole_OfLastActiveAdmin_IsConflict()
    {
        using var app = new TestApp();
        app.Users.Add(SecondAdmin, TestApp.AcmeId, DomainRole.Admin);
        app.Users.Update(TestUsers.AcmeAdmin, user => user with { IsActive = false });
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.UserAdmin.UpdateRoleAsync(SecondAdmin, ToMember, Ct);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(DomainRole.Admin, app.Users.Get(SecondAdmin).OrganizationRole);
    }

    [Fact]
    public async Task UpdateStatus_OfLastActiveAdmin_IsConflict()
    {
        using var app = new TestApp();
        app.Users.Add(SecondAdmin, TestApp.AcmeId, DomainRole.Admin);
        app.Users.Update(TestUsers.AcmeAdmin, user => user with { IsActive = false });
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.UserAdmin.UpdateStatusAsync(SecondAdmin, Deactivate, Ct);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.True(app.Users.Get(SecondAdmin).IsActive);
    }

    [Fact]
    public async Task UpdateStatus_OfOwnAccount_IsConflict()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.UserAdmin.UpdateStatusAsync(TestUsers.AcmeAdmin, Deactivate, Ct);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task UpdateStatus_OfProjectOwner_IsConflictUntilOwnershipIsTransferred()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var blocked = await app.UserAdmin.UpdateStatusAsync(TestUsers.Owner, Deactivate, Ct);
        Assert.Equal(ResultStatus.Conflict, blocked.Status);
        Assert.True(app.Users.Get(TestUsers.Owner).IsActive);

        await app.Members.TransferOwnerAsync(
            TestApp.AcmeProjectId, new Contracts.Projects.TransferProjectOwnerRequest(TestUsers.Contributor), Ct);

        var allowed = await app.UserAdmin.UpdateStatusAsync(TestUsers.Owner, Deactivate, Ct);
        Assert.True(allowed.IsSuccess);
        Assert.False(app.Users.Get(TestUsers.Owner).IsActive);
    }

    [Fact]
    public async Task ResetPassword_OfOwnAccount_IsConflict()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        var result = await app.UserAdmin.ResetPasswordAsync(TestUsers.AcmeAdmin, new ResetPasswordRequest("N3w!Pass"), Ct);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task ResetPassword_RotatesTheSecurityStamp()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);
        var stampBefore = app.Users.Get(TestUsers.Viewer).SecurityStamp;

        var result = await app.UserAdmin.ResetPasswordAsync(TestUsers.Viewer, new ResetPasswordRequest("N3w!Pass"), Ct);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(stampBefore, app.Users.Get(TestUsers.Viewer).SecurityStamp);
    }

    [Fact]
    public async Task AdminChanges_RunInsideTheAdminGuard()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.AcmeAdmin);

        await app.UserAdmin.UpdateRoleAsync(TestUsers.Viewer, new UpdateUserRoleRequest(OrganizationRole.Admin), Ct);
        await app.UserAdmin.UpdateStatusAsync(TestUsers.Outsider, Deactivate, Ct);

        Assert.Equal(2, app.Users.GuardRuns);
    }
}
