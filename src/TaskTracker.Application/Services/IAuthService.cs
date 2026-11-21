using TaskTracker.Application.Common;
using TaskTracker.Contracts.Auth;
using TaskTracker.Contracts.Users;

namespace TaskTracker.Application.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<Result<CurrentUserDto>> GetCurrentUserAsync(CancellationToken ct);

    /// <returns>A new token, because changing the password invalidates the current one.</returns>
    Task<Result<LoginResponse>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct);
}
