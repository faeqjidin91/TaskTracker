using TaskTracker.Contracts.Projects;

namespace TaskTracker.Web.Services;

public sealed class ProjectApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<ApiResult<List<ProjectDto>>> GetAllAsync() =>
        GetAsync<List<ProjectDto>>("api/projects");

    public Task<ApiResult<ProjectDto>> GetByIdAsync(int id) =>
        GetAsync<ProjectDto>($"api/projects/{id}");

    public Task<ApiResult<ProjectDto>> CreateAsync(CreateProjectRequest request) =>
        PostAsync<CreateProjectRequest, ProjectDto>("api/projects", request);

    public Task<ApiResult> UpdateAsync(int id, UpdateProjectRequest request) =>
        PutAsync($"api/projects/{id}", request);

    public Task<ApiResult> DeleteAsync(int id) =>
        SendDeleteAsync($"api/projects/{id}");

    public Task<ApiResult<List<ProjectMemberDto>>> GetMembersAsync(int id) =>
        GetAsync<List<ProjectMemberDto>>($"api/projects/{id}/members");

    public Task<ApiResult<List<ProjectCandidateDto>>> GetCandidatesAsync(int id) =>
        GetAsync<List<ProjectCandidateDto>>($"api/projects/{id}/member-candidates");

    public Task<ApiResult<ProjectMemberDto>> AddMemberAsync(int id, string userId, ProjectRole role) =>
        PostAsync<AddProjectMemberRequest, ProjectMemberDto>(
            $"api/projects/{id}/members", new AddProjectMemberRequest(userId, role));

    public Task<ApiResult> UpdateMemberAsync(int id, string userId, ProjectRole role) =>
        PatchAsync($"api/projects/{id}/members/{userId}", new UpdateProjectMemberRequest(role));

    public Task<ApiResult> RemoveMemberAsync(int id, string userId) =>
        SendDeleteAsync($"api/projects/{id}/members/{userId}");

    public Task<ApiResult> TransferOwnerAsync(int id, string ownerUserId) =>
        PatchAsync($"api/projects/{id}/owner", new TransferProjectOwnerRequest(ownerUserId));
}
