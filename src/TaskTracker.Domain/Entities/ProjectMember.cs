using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities;

public class ProjectMember
{
    // Stored here so the database can guarantee the member belongs to the project's organization.
    public int OrganizationId { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public required string UserId { get; set; }
    public ProjectRole Role { get; set; } = ProjectRole.Contributor;
    public DateTime AddedAtUtc { get; set; }
}
