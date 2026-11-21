using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities;

public class OrganizationMember
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public required string UserId { get; set; }
    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
}
