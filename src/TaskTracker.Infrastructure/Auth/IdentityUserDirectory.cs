using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Common;
using TaskTracker.Domain.Enums;
using TaskTracker.Infrastructure.Persistence;

namespace TaskTracker.Infrastructure.Auth;

public sealed class IdentityUserDirectory(
    AppDbContext db,
    UserManager<IdentityUser> users,
    TimeProvider timeProvider) : IUserDirectory
{
    private const string DuplicateEmailMessage = "A user with this email already exists.";

    // An app lock serializes admin changes across requests and API instances.
    private const string AdminGuardSql =
        """
        DECLARE @result int;
        EXEC @result = sp_getapplock @Resource = N'TaskTracker.AdminGuard', @LockMode = 'Exclusive',
            @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @result < 0 THROW 50001, 'Could not acquire the admin guard lock.', 1;
        """;

    public async Task<IReadOnlyList<UserRecord>> GetAllAsync(CancellationToken ct)
    {
        var rows = await QueryRows().OrderBy(u => u.Email).ToListAsync(ct);
        return rows.Select(ToRecord).ToList();
    }

    public async Task<UserRecord?> GetByIdAsync(string id, CancellationToken ct)
    {
        var row = await QueryRows().Where(u => u.Id == id).FirstOrDefaultAsync(ct);
        return row is null ? null : ToRecord(row);
    }

    public Task<int> CountActiveAdminsAsync(CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var adminRole = nameof(UserRole.Admin);

        return QueryRows()
            .Where(u => u.RoleName == adminRole && (u.LockoutEnd == null || u.LockoutEnd <= now))
            .CountAsync(ct);
    }

    public Task<Result<UserRecord>> CreateAsync(string email, string password, UserRole role, CancellationToken ct) =>
        InTransactionAsync(async () =>
        {
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };

            IdentityResult created;
            try
            {
                created = await users.CreateAsync(user, password);
            }
            catch (DbUpdateException)
            {
                // Two simultaneous requests can both pass the check and hit the unique index.
                return Result<UserRecord>.Conflict(DuplicateEmailMessage);
            }

            if (!created.Succeeded)
            {
                return ToFailure(created, passwordField: "Password");
            }

            var added = await users.AddToRoleAsync(user, role.ToString());
            return added.Succeeded ? await LoadAsync(user.Id, ct) : ToFailure(added, passwordField: "Password");
        }, ct);

    public Task<Result<UserRecord>> SetRoleAsync(string id, UserRole role, CancellationToken ct) =>
        InTransactionAsync(() => WithUserAsync(id, async user =>
        {
            var removed = await users.RemoveFromRolesAsync(user, await users.GetRolesAsync(user));
            if (!removed.Succeeded)
            {
                return removed;
            }

            var added = await users.AddToRoleAsync(user, role.ToString());
            return added.Succeeded ? await users.UpdateSecurityStampAsync(user) : added;
        }, "Role", ct), ct);

    public Task<Result<UserRecord>> SetActiveAsync(string id, bool isActive, CancellationToken ct) =>
        InTransactionAsync(() => WithUserAsync(id, async user =>
        {
            var enabled = await users.SetLockoutEnabledAsync(user, true);
            if (!enabled.Succeeded)
            {
                return enabled;
            }

            // A permanent lockout marks the account as deactivated, with no extra column.
            var locked = await users.SetLockoutEndDateAsync(user, isActive ? null : DateTimeOffset.MaxValue);
            return locked.Succeeded ? await users.UpdateSecurityStampAsync(user) : locked;
        }, "IsActive", ct), ct);

    public Task<Result<UserRecord>> ResetPasswordAsync(string id, string newPassword, CancellationToken ct) =>
        // The transaction restores the old password if the new one fails the strength rules.
        InTransactionAsync(() => WithUserAsync(id, async user =>
        {
            var removed = await users.RemovePasswordAsync(user);
            return removed.Succeeded ? await users.AddPasswordAsync(user, newPassword) : removed;
        }, "NewPassword", ct), ct);

    public Task<Result<UserRecord>> ChangePasswordAsync(
        string id, string currentPassword, string newPassword, CancellationToken ct) =>
        WithUserAsync(
            id,
            user => users.ChangePasswordAsync(user, currentPassword, newPassword),
            "NewPassword",
            ct);

    public Task<Result<Unit>> RunWithAdminGuardAsync(Func<Task<Result<Unit>>> action, CancellationToken ct) =>
        InTransactionAsync(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(AdminGuardSql, ct);
            return await action();
        }, ct);

    private async Task<Result<UserRecord>> WithUserAsync(
        string id,
        Func<IdentityUser, Task<IdentityResult>> change,
        string errorField,
        CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null)
        {
            return Result<UserRecord>.NotFound();
        }

        var result = await change(user);
        return result.Succeeded ? await LoadAsync(user.Id, ct) : ToFailure(result, errorField);
    }

    private async Task<Result<UserRecord>> LoadAsync(string id, CancellationToken ct)
    {
        var record = await GetByIdAsync(id, ct);
        return record is null ? Result<UserRecord>.NotFound() : Result<UserRecord>.Success(record);
    }

    private async Task<Result<T>> InTransactionAsync<T>(Func<Task<Result<T>>> action, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            return await action();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var result = await action();

        if (result.IsSuccess)
        {
            await transaction.CommitAsync(ct);
        }
        else
        {
            await transaction.RollbackAsync(ct);
        }

        return result;
    }

    private IQueryable<UserRow> QueryRows() =>
        db.Users.AsNoTracking().Select(u => new UserRow
        {
            Id = u.Id,
            Email = u.Email,
            LockoutEnd = u.LockoutEnd,
            SecurityStamp = u.SecurityStamp,
            RoleName = (from userRole in db.UserRoles
                        join role in db.Roles on userRole.RoleId equals role.Id
                        where userRole.UserId == u.Id
                        select role.Name).FirstOrDefault()
        });

    private UserRecord ToRecord(UserRow row) => new(
        row.Id,
        row.Email ?? string.Empty,
        // A user without a recognized role gets the least privilege.
        Enum.TryParse<UserRole>(row.RoleName, out var role) ? role : UserRole.Viewer,
        row.LockoutEnd is null || row.LockoutEnd <= timeProvider.GetUtcNow(),
        row.SecurityStamp ?? string.Empty);

    private static Result<UserRecord> ToFailure(IdentityResult result, string passwordField)
    {
        if (result.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail"))
        {
            return Result<UserRecord>.Conflict(DuplicateEmailMessage);
        }

        var errors = result.Errors
            .GroupBy(e => FieldFor(e.Code, passwordField))
            .ToDictionary(group => group.Key, group => group.Select(e => e.Description).ToArray());

        return Result<UserRecord>.ValidationFailed(errors);
    }

    private static string FieldFor(string code, string passwordField) => code switch
    {
        "PasswordMismatch" => "CurrentPassword",
        "InvalidEmail" or "InvalidUserName" => "Email",
        _ => passwordField
    };

    private sealed class UserRow
    {
        public required string Id { get; init; }
        public string? Email { get; init; }
        public DateTimeOffset? LockoutEnd { get; init; }
        public string? SecurityStamp { get; init; }
        public string? RoleName { get; init; }
    }
}
