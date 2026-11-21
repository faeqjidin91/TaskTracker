using Microsoft.Extensions.Logging;
using TaskTracker.Application.Abstractions;
using TaskTracker.Domain.Events;

namespace TaskTracker.Infrastructure.Notifications;

public sealed class CompositeNotificationPublisher(
    IReadOnlyList<INotificationPublisher> publishers,
    ILogger<CompositeNotificationPublisher> logger) : INotificationPublisher
{
    public Task PublishAsync(TaskStatusChangedEvent evt, CancellationToken ct) =>
        Task.WhenAll(publishers.Select(publisher => PublishSafelyAsync(publisher, evt, ct)));

    private async Task PublishSafelyAsync(
        INotificationPublisher publisher, TaskStatusChangedEvent evt, CancellationToken ct)
    {
        try
        {
            await publisher.PublishAsync(evt, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Notification provider {Provider} failed for task {TaskId}",
                publisher.GetType().Name,
                evt.TaskId);
        }
    }
}
