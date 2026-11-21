using TaskTracker.Application.Common;
using TaskTracker.Contracts.Projects;

namespace TaskTracker.Application.Services;

public interface IProjectMemberService
{
    Task<Result<IReadOnlyList<ProjectMemberDto>>> GetMembersAsync(int projectId, CancellationToken ct);
    Task<Result<IReadOnlyList<ProjectCandidateDto>>> GetCandidatesAsync(int projectId, CancellationToken ct);
    Task<Result<ProjectMemberDto>> AddAsync(int projectId, AddProjectMemberRequest request, CancellationToken ct);
    Task<Result<Unit>> UpdateAsync(int projectId, string userId, UpdateProjectMemberRequest request, CancellationToken ct);
    Task<Result<Unit>> RemoveAsync(int projectId, string userId, CancellationToken ct);
    Task<Result<Unit>> TransferOwnerAsync(int projectId, TransferProjectOwnerRequest request, CancellationToken ct);
}
