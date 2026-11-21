using TaskTracker.Application.Common;
using TaskTracker.Contracts.Auth;

namespace TaskTracker.Application.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
}
