using TaskTracker.Application.Common;
using TaskTracker.Application.Tests.Support;
using TaskTracker.Contracts.Tasks;
using DomainStatus = TaskTracker.Domain.Enums.TaskItemStatus;

namespace TaskTracker.Application.Tests;

public class TaskAccessTests
{
    private static readonly CreateTaskRequest NewTask = new("New", null, TaskPriority.Low, null);
    private static readonly UpdateTaskRequest Edit = new("Edited", null, TaskPriority.High, null);

    [Theory]
    [InlineData(TestUsers.Owner)]
    [InlineData(TestUsers.Contributor)]
    public async Task OwnerAndContributor_CanManageTasks(string userId)
    {
        using var app = new TestApp();
        app.SignInAs(userId);
        var ct = CancellationToken.None;

        var created = await app.Tasks.CreateAsync(TestApp.AcmeProjectId, NewTask, ct);
        Assert.True(created.IsSuccess);

        Assert.True((await app.Tasks.UpdateAsync(created.Value.Id, Edit, ct)).IsSuccess);
        Assert.True((await app.Tasks.UpdateStatusAsync(created.Value.Id, new UpdateTaskStatusRequest(TaskItemStatus.Done), ct)).IsSuccess);
        Assert.True((await app.Tasks.DeleteAsync(created.Value.Id, ct)).IsSuccess);
    }

    [Theory]
    [InlineData(TestUsers.Viewer)]
    [InlineData(TestUsers.AcmeAdmin)]
    public async Task ViewerAndNonMemberAdmin_CanReadButNotChangeTasks(string userId)
    {
        using var app = new TestApp();
        app.SignInAs(userId);
        var ct = CancellationToken.None;

        Assert.True((await app.Tasks.GetByProjectAsync(TestApp.AcmeProjectId, ct)).IsSuccess);
        Assert.True((await app.Tasks.GetByIdAsync(TestApp.AcmeTaskId, ct)).IsSuccess);

        Assert.Equal(ResultStatus.Forbidden, (await app.Tasks.CreateAsync(TestApp.AcmeProjectId, NewTask, ct)).Status);
        Assert.Equal(ResultStatus.Forbidden, (await app.Tasks.UpdateAsync(TestApp.AcmeTaskId, Edit, ct)).Status);
        Assert.Equal(ResultStatus.Forbidden, (await app.Tasks.UpdateStatusAsync(TestApp.AcmeTaskId, new UpdateTaskStatusRequest(TaskItemStatus.Done), ct)).Status);
        Assert.Equal(ResultStatus.Forbidden, (await app.Tasks.DeleteAsync(TestApp.AcmeTaskId, ct)).Status);

        Assert.Equal(DomainStatus.Todo, app.SavedTask(TestApp.AcmeTaskId).Status);
        Assert.Empty(app.Publisher.Events);
    }

    [Fact]
    public async Task OrganizationMemberOutsideTheProject_GetsNotFound()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Outsider);
        var ct = CancellationToken.None;

        Assert.Equal(ResultStatus.NotFound, (await app.Tasks.GetByProjectAsync(TestApp.AcmeProjectId, ct)).Status);
        Assert.Equal(ResultStatus.NotFound, (await app.Tasks.GetByIdAsync(TestApp.AcmeTaskId, ct)).Status);
        Assert.Equal(ResultStatus.NotFound, (await app.Tasks.CreateAsync(TestApp.AcmeProjectId, NewTask, ct)).Status);
        Assert.Equal(ResultStatus.NotFound, (await app.Tasks.DeleteAsync(TestApp.AcmeTaskId, ct)).Status);
    }
}
