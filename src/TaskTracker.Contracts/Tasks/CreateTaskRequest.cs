using System.Text.Json.Serialization;

namespace TaskTracker.Contracts.Tasks;

// ProjectId comes from the route. New tasks always start as Todo.
public record CreateTaskRequest(
    string Title,
    string? Description,
    [property: JsonRequired] TaskPriority Priority,
    DateTime? DueDateUtc);
