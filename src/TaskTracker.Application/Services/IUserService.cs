using TaskTracker.Application.Common;
using TaskTracker.Contracts.Users;

namespace TaskTracker.Application.Services;

public interface IUserService
{
    Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<UserDto>> GetByIdAsync(string id, CancellationToken ct);
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<Result<Unit>> UpdateRoleAsync(string id, UpdateUserRoleRequest request, CancellationToken ct);
    Task<Result<Unit>> UpdateStatusAsync(string id, UpdateUserStatusRequest request, CancellationToken ct);
    Task<Result<Unit>> ResetPasswordAsync(string id, ResetPasswordRequest request, CancellationToken ct);
}
