using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Common;
using TaskTracker.Application.Mapping;
using TaskTracker.Contracts.Users;
using DomainRole = TaskTracker.Domain.Enums.OrganizationRole;

namespace TaskTracker.Application.Services;

// Manages users in the signed-in Admin's organization only.
public class UserService(
    IUserDirectory users,
    IAppDbContext db,
    ICurrentUser currentUser,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRoleRequest> roleValidator,
    IValidator<ResetPasswordRequest> resetValidator) : IUserService
{
    private const string LastAdminMessage = "The last active admin cannot be demoted or deactivated.";

    private int OrganizationId => currentUser.OrganizationId
        ?? throw new InvalidOperationException("User management requires an organization.");

    public async Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct)
    {
        var records = await users.GetInOrganizationAsync(OrganizationId, ct);
        IReadOnlyList<UserDto> dtos = records.Select(u => u.ToDto()).ToList();
        return Result<IReadOnlyList<UserDto>>.Success(dtos);
    }

    public async Task<Result<UserDto>> GetByIdAsync(string id, CancellationToken ct)
    {
        var user = await users.GetInOrganizationAsync(OrganizationId, id, ct);
        return user is null ? Result<UserDto>.NotFound() : Result<UserDto>.Success(user.ToDto());
    }

    public async Task<Result<IReadOnlyList<OwnedProjectDto>>> GetOwnedProjectsAsync(string id, CancellationToken ct)
    {
        var user = await users.GetInOrganizationAsync(OrganizationId, id, ct);
        if (user is null)
        {
            return Result<IReadOnlyList<OwnedProjectDto>>.NotFound();
        }

        var projects = await db.Projects
            .AsNoTracking()
            .Where(p => p.OwnerUserId == user.Id)
            .OrderBy(p => p.Name)
            .Select(p => new OwnedProjectDto(p.Id, p.Name))
            .ToListAsync(ct);

        return Result<IReadOnlyList<OwnedProjectDto>>.Success(projects);
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<UserDto>.ValidationFailed(validation.ToDictionary());
        }

        var created = await users.CreateAsync(
            OrganizationId, request.Email.Trim(), request.Password, request.Role.ToDomain(), ct);

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

        var organizationId = OrganizationId;
        var newRole = request.Role.ToDomain();

        return await users.RunWithAdminGuardAsync(async () =>
        {
            var user = await users.GetInOrganizationAsync(organizationId, id, ct);
            if (user is null)
            {
                return Result<Unit>.NotFound();
            }

            if (IsCurrentUser(user))
            {
                return Result<Unit>.Conflict("You cannot change your own role.");
            }

            if (user.OrganizationRole == newRole)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            if (await IsLastActiveAdminAsync(user, organizationId, ct))
            {
                return Result<Unit>.Conflict(LastAdminMessage);
            }

            return ToUnit(await users.SetRoleAsync(organizationId, user.Id, newRole, ct));
        }, ct);
    }

    public Task<Result<Unit>> UpdateStatusAsync(string id, UpdateUserStatusRequest request, CancellationToken ct)
    {
        var organizationId = OrganizationId;

        return users.RunWithAdminGuardAsync(async () =>
        {
            var user = await users.GetInOrganizationAsync(organizationId, id, ct);
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

            if (!request.IsActive)
            {
                if (await IsLastActiveAdminAsync(user, organizationId, ct))
                {
                    return Result<Unit>.Conflict(LastAdminMessage);
                }

                var reassigned = await ReassignProjectsAsync(user, request.NewOwnerUserId, organizationId, ct);
                if (!reassigned.IsSuccess)
                {
                    return reassigned;
                }
            }

            return ToUnit(await users.SetActiveAsync(organizationId, user.Id, request.IsActive, ct));
        }, ct);
    }

    public async Task<Result<Unit>> ResetPasswordAsync(string id, ResetPasswordRequest request, CancellationToken ct)
    {
        var validation = await resetValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailed(validation.ToDictionary());
        }

        var user = await users.GetInOrganizationAsync(OrganizationId, id, ct);
        if (user is null)
        {
            return Result<Unit>.NotFound();
        }

        if (IsCurrentUser(user))
        {
            return Result<Unit>.Conflict("Use change password for your own account.");
        }

        return ToUnit(await users.ResetPasswordAsync(OrganizationId, user.Id, request.NewPassword, ct));
    }

    // Runs inside the admin guard transaction, so a failed deactivation also undoes the transfer.
    private async Task<Result<Unit>> ReassignProjectsAsync(
        UserRecord user, string? newOwnerUserId, int organizationId, CancellationToken ct)
    {
        var owned = await db.Projects
            .Include(p => p.Members)
            .Where(p => p.OwnerUserId == user.Id)
            .ToListAsync(ct);

        if (owned.Count == 0)
        {
            return Result<Unit>.Success(Unit.Value);
        }

        if (string.IsNullOrWhiteSpace(newOwnerUserId))
        {
            return Result<Unit>.Conflict("Choose a new owner for this user's projects first.");
        }

        var newOwner = await users.GetInOrganizationAsync(organizationId, newOwnerUserId, ct);
        if (newOwner is not { IsActive: true } || newOwner.Id == user.Id)
        {
            return Result<Unit>.Invalid(
                nameof(UpdateUserStatusRequest.NewOwnerUserId),
                "The new owner must be another active user in this organization.");
        }

        foreach (var project in owned)
        {
            // The new owner's access now comes from ownership, so a member row would be redundant.
            var existing = project.Members.FirstOrDefault(m => m.UserId == newOwner.Id);
            if (existing is not null)
            {
                db.ProjectMembers.Remove(existing);
            }

            project.OwnerUserId = newOwner.Id;
        }

        await db.SaveChangesAsync(ct);
        return Result<Unit>.Success(Unit.Value);
    }

    // Compares the stored id, because SQL Server matches ids case-insensitively in the route.
    private bool IsCurrentUser(UserRecord user) =>
        string.Equals(user.Id, currentUser.Id, StringComparison.OrdinalIgnoreCase);

    private async Task<bool> IsLastActiveAdminAsync(UserRecord user, int organizationId, CancellationToken ct) =>
        user is { OrganizationRole: DomainRole.Admin, IsActive: true }
        && await users.CountActiveAdminsAsync(organizationId, ct) <= 1;

    private static Result<Unit> ToUnit(Result<UserRecord> result) =>
        result.IsSuccess ? Result<Unit>.Success(Unit.Value) : result.ToFailure<Unit>();
}
