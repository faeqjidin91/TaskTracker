using TaskTracker.Contracts.Users;

namespace TaskTracker.Web.Services;

public sealed class UserApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<ApiResult<List<UserDto>>> GetAllAsync() =>
        GetAsync<List<UserDto>>("api/users");

    public Task<ApiResult<UserDto>> CreateAsync(CreateUserRequest request) =>
        PostAsync<CreateUserRequest, UserDto>("api/users", request);

    public Task<ApiResult> UpdateRoleAsync(string id, OrganizationRole role) =>
        PatchAsync($"api/users/{id}/role", new UpdateUserRoleRequest(role));

    public Task<ApiResult<List<OwnedProjectDto>>> GetOwnedProjectsAsync(string id) =>
        GetAsync<List<OwnedProjectDto>>($"api/users/{id}/owned-projects");

    public Task<ApiResult> UpdateStatusAsync(string id, bool isActive, string? newOwnerUserId = null) =>
        PatchAsync($"api/users/{id}/status", new UpdateUserStatusRequest(isActive, newOwnerUserId));

    public Task<ApiResult> ResetPasswordAsync(string id, string newPassword) =>
        PostNoContentAsync($"api/users/{id}/reset-password", new ResetPasswordRequest(newPassword));
}
