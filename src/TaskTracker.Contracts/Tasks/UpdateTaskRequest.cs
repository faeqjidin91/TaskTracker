namespace TaskTracker.Contracts.Tasks;

// Status is excluded: it changes only through UpdateTaskStatusRequest, so every status
// change goes through the one code path that publishes a notification.
public record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime? DueDateUtc);
