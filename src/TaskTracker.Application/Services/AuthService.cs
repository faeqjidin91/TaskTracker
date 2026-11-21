using FluentValidation;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Common;
using TaskTracker.Application.Mapping;
using TaskTracker.Contracts.Auth;
using TaskTracker.Contracts.Users;

namespace TaskTracker.Application.Services;

public class AuthService(
    IUserAuthenticator authenticator,
    IUserDirectory users,
    ITokenService tokenService,
    ICurrentUser currentUser,
    IValidator<LoginRequest> loginValidator,
    IValidator<ChangePasswordRequest> changePasswordValidator) : IAuthService
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var validation = await loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<LoginResponse>.ValidationFailed(validation.ToDictionary());
        }

        var user = await authenticator.AuthenticateAsync(request.Email, request.Password, ct);

        return user is null
            ? Result<LoginResponse>.Unauthorized()
            : Result<LoginResponse>.Success(CreateLoginResponse(user));
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(CancellationToken ct)
    {
        if (currentUser.Id is not { } id)
        {
            return Result<UserDto>.Unauthorized();
        }

        var user = await users.GetByIdAsync(id, ct);

        return user is null
            ? Result<UserDto>.Unauthorized()
            : Result<UserDto>.Success(user.ToDto());
    }

    public async Task<Result<LoginResponse>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        var validation = await changePasswordValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<LoginResponse>.ValidationFailed(validation.ToDictionary());
        }

        if (currentUser.Id is not { } id)
        {
            return Result<LoginResponse>.Unauthorized();
        }

        var changed = await users.ChangePasswordAsync(id, request.CurrentPassword, request.NewPassword, ct);

        return changed.IsSuccess
            ? Result<LoginResponse>.Success(CreateLoginResponse(changed.Value))
            : changed.ToFailure<LoginResponse>();
    }

    private LoginResponse CreateLoginResponse(UserRecord user)
    {
        var token = tokenService.CreateToken(user);
        return new LoginResponse(token.Token, token.ExpiresAtUtc, user.Email);
    }
}
