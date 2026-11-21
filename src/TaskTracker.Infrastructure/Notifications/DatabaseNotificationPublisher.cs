using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Application.Abstractions;
using TaskTracker.Domain.Events;
using TaskTracker.Infrastructure.Persistence;

namespace TaskTracker.Infrastructure.Notifications;

public sealed class DatabaseNotificationPublisher(IServiceScopeFactory scopeFactory) : INotificationPublisher
{
    public async Task PublishAsync(TaskStatusChangedEvent evt, CancellationToken ct)
    {
        // A separate DbContext keeps a failed insert out of the request's change tracker.
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.NotificationLogs.Add(new NotificationLog
        {
            OrganizationId = evt.OrganizationId,
            TaskId = evt.TaskId,
            Message = $"Task {evt.TaskId} \"{evt.Title}\" changed from {evt.OldStatus} to {evt.NewStatus} by {evt.ChangedBy}",
            CreatedAtUtc = evt.OccurredAtUtc
        });

        await db.SaveChangesAsync(ct);
    }
}
