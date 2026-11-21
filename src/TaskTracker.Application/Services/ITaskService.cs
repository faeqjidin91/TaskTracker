using TaskTracker.Application.Common;
using TaskTracker.Contracts.Tasks;

namespace TaskTracker.Application.Services;

public interface ITaskService
{
    Task<Result<IReadOnlyList<TaskDto>>> GetByProjectAsync(int projectId, CancellationToken ct);
    Task<Result<TaskDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<TaskDto>> CreateAsync(int projectId, CreateTaskRequest request, CancellationToken ct);
    Task<Result<Unit>> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken ct);
    Task<Result<Unit>> UpdateStatusAsync(int id, UpdateTaskStatusRequest request, CancellationToken ct);
    Task<Result<Unit>> DeleteAsync(int id, CancellationToken ct);
}
