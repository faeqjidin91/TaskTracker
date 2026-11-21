using TaskTracker.Contracts.Tasks;

namespace TaskTracker.Web.Services;

public sealed class TaskApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<ApiResult<List<TaskDto>>> GetByProjectAsync(int projectId) =>
        GetAsync<List<TaskDto>>($"api/projects/{projectId}/tasks");

    public Task<ApiResult<TaskDto>> CreateAsync(int projectId, CreateTaskRequest request) =>
        PostAsync<CreateTaskRequest, TaskDto>($"api/projects/{projectId}/tasks", request);

    public Task<ApiResult> UpdateStatusAsync(int id, TaskItemStatus status) =>
        PatchAsync($"api/tasks/{id}/status", new UpdateTaskStatusRequest(status));

    public Task<ApiResult> DeleteAsync(int id) =>
        SendDeleteAsync($"api/tasks/{id}");
}
