namespace TaskTracker.Domain.Entities;

public class Organization
{
    public const int NameMaxLength = 200;

    public int Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
}
