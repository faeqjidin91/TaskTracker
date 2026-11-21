namespace TaskTracker.Application.Abstractions;

public interface ICurrentUser
{
    string? Id { get; }
    string? Email { get; }
    int? OrganizationId { get; }
    bool IsOrganizationAdmin { get; }
    bool IsSuperAdmin { get; }
}
