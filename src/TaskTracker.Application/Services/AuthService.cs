using FluentValidation;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Common;
using TaskTracker.Contracts.Auth;

namespace TaskTracker.Application.Services;

public class AuthService(
    IUserAuthenticator authenticator,
    ITokenService tokenService,
    IValidator<LoginRequest> validator) : IAuthService
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<LoginResponse>.ValidationFailed(validation.ToDictionary());
        }

        var user = await authenticator.AuthenticateAsync(request.Email, request.Password, ct);
        if (user is null)
        {
            return Result<LoginResponse>.Unauthorized();
        }

        var token = tokenService.CreateToken(user.Id, user.Email);
        return Result<LoginResponse>.Success(new LoginResponse(token.Token, token.ExpiresAtUtc, user.Email));
    }
}
