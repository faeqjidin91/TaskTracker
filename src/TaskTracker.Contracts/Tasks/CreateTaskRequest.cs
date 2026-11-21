namespace TaskTracker.Contracts.Tasks;

// ProjectId comes from the route. New tasks always start as Todo.
public record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime? DueDateUtc);
