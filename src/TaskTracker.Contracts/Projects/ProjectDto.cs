namespace TaskTracker.Contracts.Projects;

public record ProjectDto(
    int Id,
    string Name,
    string? Description,
    int TaskCount,
    DateTime CreatedAtUtc);
