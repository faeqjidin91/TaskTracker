using TaskTracker.Contracts.Organizations;
using TaskTracker.Contracts.Users;

namespace TaskTracker.Web.Services;

public sealed class OrganizationApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<ApiResult<List<OrganizationDto>>> GetAllAsync() =>
        GetAsync<List<OrganizationDto>>("api/organizations");

    public Task<ApiResult<OrganizationDto>> CreateAsync(CreateOrganizationRequest request) =>
        PostAsync<CreateOrganizationRequest, OrganizationDto>("api/organizations", request);

    public Task<ApiResult> UpdateStatusAsync(int id, bool isActive) =>
        PatchAsync($"api/organizations/{id}/status", new UpdateOrganizationStatusRequest(isActive));

    public Task<ApiResult<UserDto>> AddAdminAsync(int id, string email, string password) =>
        PostAsync<AddOrganizationAdminRequest, UserDto>(
            $"api/organizations/{id}/admins", new AddOrganizationAdminRequest(email, password));
}
