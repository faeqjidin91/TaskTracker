using FluentValidation;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Common;
using TaskTracker.Application.Mapping;
using TaskTracker.Contracts.Users;
using DomainRole = TaskTracker.Domain.Enums.UserRole;

namespace TaskTracker.Application.Services;

public class UserService(
    IUserDirectory users,
    ICurrentUser currentUser,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRoleRequest> roleValidator,
    IValidator<ResetPasswordRequest> resetValidator) : IUserService
{
    private const string LastAdminMessage = "The last active admin cannot be demoted or deactivated.";

    public async Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct)
    {
        var records = await users.GetAllAsync(ct);
        IReadOnlyList<UserDto> dtos = records.Select(u => u.ToDto()).ToList();
        return Result<IReadOnlyList<UserDto>>.Success(dtos);
    }

    public async Task<Result<UserDto>> GetByIdAsync(string id, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(id, ct);
        return user is null ? Result<UserDto>.NotFound() : Result<UserDto>.Success(user.ToDto());
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<UserDto>.ValidationFailed(validation.ToDictionary());
        }

        var created = await users.CreateAsync(request.Email.Trim(), request.Password, request.Role.ToDomain(), ct);

        return created.IsSuccess
            ? Result<UserDto>.Success(created.Value.ToDto())
            : created.ToFailure<UserDto>();
    }

    public async Task<Result<Unit>> UpdateRoleAsync(string id, UpdateUserRoleRequest request, CancellationToken ct)
    {
        var validation = await roleValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailed(validation.ToDictionary());
        }

        var newRole = request.Role.ToDomain();

        return await users.RunWithAdminGuardAsync(async () =>
        {
            var user = await users.GetByIdAsync(id, ct);
            if (user is null)
            {
                return Result<Unit>.NotFound();
            }

            if (IsCurrentUser(user))
            {
                return Result<Unit>.Conflict("You cannot change your own role.");
            }

            if (user.Role == newRole)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            if (await IsLastActiveAdminAsync(user, ct))
            {
                return Result<Unit>.Conflict(LastAdminMessage);
            }

            return ToUnit(await users.SetRoleAsync(user.Id, newRole, ct));
        }, ct);
    }

    public Task<Result<Unit>> UpdateStatusAsync(string id, UpdateUserStatusRequest request, CancellationToken ct) =>
        users.RunWithAdminGuardAsync(async () =>
        {
            var user = await users.GetByIdAsync(id, ct);
            if (user is null)
            {
                return Result<Unit>.NotFound();
            }

            if (IsCurrentUser(user))
            {
                return Result<Unit>.Conflict("You cannot change your own status.");
            }

            if (user.IsActive == request.IsActive)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            if (!request.IsActive && await IsLastActiveAdminAsync(user, ct))
            {
                return Result<Unit>.Conflict(LastAdminMessage);
            }

            return ToUnit(await users.SetActiveAsync(user.Id, request.IsActive, ct));
        }, ct);

    public async Task<Result<Unit>> ResetPasswordAsync(string id, ResetPasswordRequest request, CancellationToken ct)
    {
        var validation = await resetValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailed(validation.ToDictionary());
        }

        var user = await users.GetByIdAsync(id, ct);
        if (user is null)
        {
            return Result<Unit>.NotFound();
        }

        if (IsCurrentUser(user))
        {
            return Result<Unit>.Conflict("Use change password for your own account.");
        }

        return ToUnit(await users.ResetPasswordAsync(user.Id, request.NewPassword, ct));
    }

    // Compares the stored id, because SQL Server matches ids case-insensitively in the route.
    private bool IsCurrentUser(UserRecord user) =>
        string.Equals(user.Id, currentUser.Id, StringComparison.OrdinalIgnoreCase);

    private async Task<bool> IsLastActiveAdminAsync(UserRecord user, CancellationToken ct) =>
        user is { Role: DomainRole.Admin, IsActive: true } && await users.CountActiveAdminsAsync(ct) <= 1;

    private static Result<Unit> ToUnit(Result<UserRecord> result) =>
        result.IsSuccess ? Result<Unit>.Success(Unit.Value) : result.ToFailure<Unit>();
}
