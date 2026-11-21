using TaskTracker.Contracts.Projects;

namespace TaskTracker.Web.Services;

public sealed class ProjectApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<ApiResult<List<ProjectDto>>> GetAllAsync() =>
        GetAsync<List<ProjectDto>>("api/projects");

    public Task<ApiResult<ProjectDto>> GetByIdAsync(int id) =>
        GetAsync<ProjectDto>($"api/projects/{id}");
}
