namespace TaskTracker.Domain.Entities;

public class Project
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public required string OwnerUserId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
}
