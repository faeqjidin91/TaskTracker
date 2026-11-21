using System.Text.Json.Serialization;

namespace TaskTracker.Contracts.Projects;

// Duplicates the Domain enum for the same reason as TaskItemStatus: Web cannot see Domain.
public enum ProjectRole
{
    Viewer = 0,
    Contributor = 1
}

public record ProjectMemberDto(string UserId, string Email, ProjectRole Role);

public record ProjectCandidateDto(string UserId, string Email);

public record AddProjectMemberRequest(string UserId, [property: JsonRequired] ProjectRole Role);

public record UpdateProjectMemberRequest([property: JsonRequired] ProjectRole Role);

public record TransferProjectOwnerRequest(string OwnerUserId);
