using TaskTracker.Application.Common;
using TaskTracker.Application.Tests.Support;
using TaskTracker.Contracts.Organizations;
using DomainRole = TaskTracker.Domain.Enums.OrganizationRole;

namespace TaskTracker.Application.Tests;

public class OrganizationServiceTests
{
    private const string Password = "Str0ng!Pass";
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task Create_AddsOrganizationWithItsFirstAdmin()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.SuperAdmin);

        var result = await app.Organizations.CreateAsync(
            new CreateOrganizationRequest("  Initech  ", "first@test.local", Password), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Initech", result.Value.Name);
        Assert.Equal(1, result.Value.UserCount);
        var admin = Assert.Single(await app.Users.GetInOrganizationAsync(result.Value.Id, Ct));
        Assert.Equal(DomainRole.Admin, admin.OrganizationRole);
    }

    [Fact]
    public async Task Create_WithDuplicateName_IsConflict()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.SuperAdmin);

        var result = await app.Organizations.CreateAsync(
            new CreateOrganizationRequest("Acme", "first@test.local", Password), Ct);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Create_WhenFirstAdminIsRejected_ReturnsTheFailure()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.SuperAdmin);

        var result = await app.Organizations.CreateAsync(
            new CreateOrganizationRequest("Initech", "first@test.local", FakeUserDirectory.WeakPassword), Ct);

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
    }

    [Theory]
    [InlineData("", "first@test.local", Password)]
    [InlineData("Initech", "not-an-email", Password)]
    [InlineData("Initech", "first@test.local", "")]
    public async Task Create_WithInvalidRequest_ReturnsValidationFailed(string name, string email, string password)
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.SuperAdmin);

        var result = await app.Organizations.CreateAsync(new CreateOrganizationRequest(name, email, password), Ct);

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryOrganizationWithUserCounts()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.SuperAdmin);

        var result = await app.Organizations.GetAllAsync(Ct);

        Assert.Equal(2, result.Value.Count);
        Assert.Equal(5, result.Value.Single(o => o.Id == TestApp.AcmeId).UserCount);
        Assert.Equal(1, result.Value.Single(o => o.Id == TestApp.GlobexId).UserCount);
    }

    [Fact]
    public async Task UpdateStatus_ChangesTheActiveFlag()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.SuperAdmin);

        var result = await app.Organizations.UpdateStatusAsync(TestApp.AcmeId, new UpdateOrganizationStatusRequest(false), Ct);

        Assert.True(result.IsSuccess);
        Assert.False(app.Saved(db => db.Organizations.Single(o => o.Id == TestApp.AcmeId).IsActive));
    }

    [Fact]
    public async Task UpdateStatus_OfMissingOrganization_ReturnsNotFound()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.SuperAdmin);

        var result = await app.Organizations.UpdateStatusAsync(9999, new UpdateOrganizationStatusRequest(false), Ct);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task AddAdmin_AddsAnAdminToTheOrganization()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.SuperAdmin);

        var result = await app.Organizations.AddAdminAsync(
            TestApp.GlobexId, new AddOrganizationAdminRequest("second@test.local", Password), Ct);

        Assert.True(result.IsSuccess);
        var added = app.Users.Get(result.Value.Id);
        Assert.Equal(TestApp.GlobexId, added.OrganizationId);
        Assert.Equal(DomainRole.Admin, added.OrganizationRole);
    }

    [Fact]
    public async Task AddAdmin_ToMissingOrganization_ReturnsNotFound()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.SuperAdmin);

        var result = await app.Organizations.AddAdminAsync(
            9999, new AddOrganizationAdminRequest("second@test.local", Password), Ct);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
