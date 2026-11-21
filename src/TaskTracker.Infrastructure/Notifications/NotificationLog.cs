namespace TaskTracker.Infrastructure.Notifications;

public class NotificationLog
{
    public const int MessageMaxLength = 1000;

    public int Id { get; set; }
    public int OrganizationId { get; set; }

    // Deliberately not a foreign key, so the history survives when a task is deleted.
    public int TaskId { get; set; }
    public required string Message { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
