using TaskTracker.Application.Common;
using TaskTracker.Contracts.Organizations;
using TaskTracker.Contracts.Users;

namespace TaskTracker.Application.Services;

public interface IOrganizationService
{
    Task<Result<IReadOnlyList<OrganizationDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<OrganizationDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<OrganizationDto>> CreateAsync(CreateOrganizationRequest request, CancellationToken ct);
    Task<Result<Unit>> UpdateStatusAsync(int id, UpdateOrganizationStatusRequest request, CancellationToken ct);
    Task<Result<UserDto>> AddAdminAsync(int id, AddOrganizationAdminRequest request, CancellationToken ct);
}
