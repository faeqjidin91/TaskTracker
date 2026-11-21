using Microsoft.Extensions.Logging;
using TaskTracker.Application.Abstractions;
using TaskTracker.Domain.Events;

namespace TaskTracker.Infrastructure.Notifications;

public sealed class LoggingNotificationPublisher(ILogger<LoggingNotificationPublisher> logger)
    : INotificationPublisher
{
    public Task PublishAsync(TaskStatusChangedEvent evt, CancellationToken ct)
    {
        logger.LogInformation(
            "Task {TaskId} in project {ProjectId} changed from {OldStatus} to {NewStatus} by {ChangedBy}",
            evt.TaskId,
            evt.ProjectId,
            evt.OldStatus,
            evt.NewStatus,
            evt.ChangedBy);

        return Task.CompletedTask;
    }
}
