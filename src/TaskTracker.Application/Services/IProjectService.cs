using TaskTracker.Application.Common;
using TaskTracker.Contracts.Projects;

namespace TaskTracker.Application.Services;

public interface IProjectService
{
    Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<ProjectDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<ProjectDto>> CreateAsync(CreateProjectRequest request, CancellationToken ct);
    Task<Result<Unit>> UpdateAsync(int id, UpdateProjectRequest request, CancellationToken ct);
    Task<Result<Unit>> DeleteAsync(int id, CancellationToken ct);
}
