using TaskTracker.Domain.Events;

namespace TaskTracker.Application.Abstractions;

public interface INotificationPublisher
{
    Task PublishAsync(TaskStatusChangedEvent evt, CancellationToken ct);
}
