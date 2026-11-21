using Microsoft.Extensions.Logging;
using TaskTracker.Application.Common;
using TaskTracker.Application.Tests.Support;
using TaskTracker.Contracts.Tasks;
using DomainStatus = TaskTracker.Domain.Enums.TaskItemStatus;

namespace TaskTracker.Application.Tests;

public class TaskStatusNotificationTests
{
    private static readonly UpdateTaskStatusRequest ToDone = new(TaskItemStatus.Done);

    [Fact]
    public async Task UpdateStatus_WhenStatusChanges_PublishesEvent()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Contributor);

        var result = await app.Tasks.UpdateStatusAsync(TestApp.AcmeTaskId, ToDone, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var evt = Assert.Single(app.Publisher.Events);
        Assert.Equal(TestApp.AcmeTaskId, evt.TaskId);
        Assert.Equal(TestApp.AcmeProjectId, evt.ProjectId);
        Assert.Equal(TestApp.AcmeId, evt.OrganizationId);
        Assert.Equal("Build", evt.Title);
        Assert.Equal(DomainStatus.Todo, evt.OldStatus);
        Assert.Equal(DomainStatus.Done, evt.NewStatus);
        Assert.Equal($"{TestUsers.Contributor}@test.local", evt.ChangedBy);
        Assert.Equal(TestApp.Now, evt.OccurredAtUtc);
    }

    [Fact]
    public async Task UpdateStatus_PublishesOnlyAfterTheChangeIsSaved()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Contributor);
        DomainStatus? statusSeenByPublisher = null;
        app.Publisher.OnPublish = evt => statusSeenByPublisher = app.SavedTask(evt.TaskId).Status;

        await app.Tasks.UpdateStatusAsync(TestApp.AcmeTaskId, ToDone, CancellationToken.None);

        Assert.Equal(DomainStatus.Done, statusSeenByPublisher);
    }

    [Fact]
    public async Task UpdateStatus_WhenStatusIsUnchanged_DoesNotPublishOrTouchTheTask()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Contributor);

        var result = await app.Tasks.UpdateStatusAsync(
            TestApp.AcmeTaskId, new UpdateTaskStatusRequest(TaskItemStatus.Todo), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(app.Publisher.Events);
        Assert.Equal(TestApp.SeedDate, DateTime.SpecifyKind(app.SavedTask(TestApp.AcmeTaskId).UpdatedAtUtc, DateTimeKind.Utc));
    }

    [Fact]
    public async Task UpdateStatus_WhenPublisherThrows_StillSucceedsSavesAndLogsWarning()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Contributor);
        app.Publisher.ThrowOnPublish = true;

        var result = await app.Tasks.UpdateStatusAsync(TestApp.AcmeTaskId, ToDone, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DomainStatus.Done, app.SavedTask(TestApp.AcmeTaskId).Status);
        Assert.Contains(app.TaskLogger.Entries, entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains(TestApp.AcmeTaskId.ToString()));
    }

    [Fact]
    public async Task UpdateStatus_WhenSaveFails_DoesNotPublish()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Contributor);
        app.Db.FailNextSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            app.Tasks.UpdateStatusAsync(TestApp.AcmeTaskId, ToDone, CancellationToken.None));

        Assert.Empty(app.Publisher.Events);
        Assert.Equal(DomainStatus.Todo, app.SavedTask(TestApp.AcmeTaskId).Status);
    }

    [Fact]
    public async Task UpdateStatus_DoesNotPassTheRequestTokenToThePublisher()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Contributor);
        using var requestAborted = new CancellationTokenSource();

        await app.Tasks.UpdateStatusAsync(TestApp.AcmeTaskId, ToDone, requestAborted.Token);

        Assert.False(app.Publisher.LastToken.CanBeCanceled);
    }

    [Fact]
    public async Task UpdateStatus_WithUndefinedStatus_ReturnsValidationFailed()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Contributor);

        var result = await app.Tasks.UpdateStatusAsync(
            TestApp.AcmeTaskId, new UpdateTaskStatusRequest((TaskItemStatus)99), CancellationToken.None);

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
        Assert.Empty(app.Publisher.Events);
    }

    [Fact]
    public async Task CreateTask_InNonExistentProject_ReturnsNotFound()
    {
        using var app = new TestApp();
        app.SignInAs(TestUsers.Owner);

        var result = await app.Tasks.CreateAsync(
            9999, new CreateTaskRequest("New", null, TaskPriority.Low, null), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
