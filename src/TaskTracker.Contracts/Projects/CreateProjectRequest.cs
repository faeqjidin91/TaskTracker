namespace TaskTracker.Contracts.Projects;

// When OwnerUserId is omitted, the creator becomes the owner. Only Admins can name someone else.
public record CreateProjectRequest(string Name, string? Description, string? OwnerUserId = null);
