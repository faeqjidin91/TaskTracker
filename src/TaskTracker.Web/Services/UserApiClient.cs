using TaskTracker.Contracts.Users;

namespace TaskTracker.Web.Services;

public sealed class UserApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<ApiResult<List<UserDto>>> GetAllAsync() =>
        GetAsync<List<UserDto>>("api/users");

    public Task<ApiResult<UserDto>> CreateAsync(CreateUserRequest request) =>
        PostAsync<CreateUserRequest, UserDto>("api/users", request);

    public Task<ApiResult> UpdateRoleAsync(string id, UserRole role) =>
        PatchAsync($"api/users/{id}/role", new UpdateUserRoleRequest(role));

    public Task<ApiResult> UpdateStatusAsync(string id, bool isActive) =>
        PatchAsync($"api/users/{id}/status", new UpdateUserStatusRequest(isActive));

    public Task<ApiResult> ResetPasswordAsync(string id, string newPassword) =>
        PostNoContentAsync($"api/users/{id}/reset-password", new ResetPasswordRequest(newPassword));
}
