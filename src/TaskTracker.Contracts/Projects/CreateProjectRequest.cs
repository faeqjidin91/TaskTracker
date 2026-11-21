namespace TaskTracker.Contracts.Projects;

// When OwnerUserId is omitted, the Admin creating the project becomes its owner.
public record CreateProjectRequest(string Name, string? Description, string? OwnerUserId = null);
