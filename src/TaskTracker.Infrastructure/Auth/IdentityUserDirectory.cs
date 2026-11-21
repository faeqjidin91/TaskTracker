using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Common;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Infrastructure.Persistence;
using TaskTracker.Infrastructure.Persistence.Seed;

namespace TaskTracker.Infrastructure.Auth;

// Organization ids are explicit here because SuperAdmins act on organizations they are not in.
public sealed class IdentityUserDirectory(
    AppDbContext db,
    UserManager<IdentityUser> users,
    TimeProvider timeProvider) : IUserDirectory
{
    private const string DuplicateEmailMessage = "A user with this email already exists.";

    // An app lock serializes membership changes across requests and API instances.
    private const string AdminGuardSql =
        """
        DECLARE @result int;
        EXEC @result = sp_getapplock @Resource = N'TaskTracker.AdminGuard', @LockMode = 'Exclusive',
            @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @result < 0 THROW 50001, 'Could not acquire the admin guard lock.', 1;
        """;

    public async Task<UserRecord?> GetByIdAsync(string id, CancellationToken ct)
    {
        var row = await QueryRows().Where(u => u.Id == id).FirstOrDefaultAsync(ct);
        return row is null ? null : ToRecord(row);
    }

    public async Task<IReadOnlyList<UserRecord>> GetInOrganizationAsync(int organizationId, CancellationToken ct)
    {
        var rows = await QueryRows()
            .Where(u => u.OrganizationId == organizationId)
            .OrderBy(u => u.Email)
            .ToListAsync(ct);

        return rows.Select(ToRecord).ToList();
    }

    public async Task<UserRecord?> GetInOrganizationAsync(int organizationId, string id, CancellationToken ct)
    {
        var row = await QueryRows()
            .Where(u => u.Id == id && u.OrganizationId == organizationId)
            .FirstOrDefaultAsync(ct);

        return row is null ? null : ToRecord(row);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetEmailsAsync(IEnumerable<string> ids, CancellationToken ct)
    {
        var idList = ids.Distinct().ToList();

        return await db.Users
            .AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? string.Empty, ct);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetOrganizationUserCountsAsync(CancellationToken ct)
    {
        return await db.OrganizationMembers
            .IgnoreQueryFilters()
            .Where(m => m.UserId != SeedIds.SystemUserId)
            .GroupBy(m => m.OrganizationId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Key, row => row.Count, ct);
    }

    public Task<int> CountActiveAdminsAsync(int organizationId, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();

        return db.OrganizationMembers
            .IgnoreQueryFilters()
            .Where(m => m.OrganizationId == organizationId && m.Role == OrganizationRole.Admin)
            .Join(db.Users, m => m.UserId, u => u.Id, (m, u) => u)
            .CountAsync(u => u.LockoutEnd == null || u.LockoutEnd <= now, ct);
    }

    public Task<Result<UserRecord>> CreateAsync(
        int organizationId, string email, string password, OrganizationRole role, CancellationToken ct) =>
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

            db.OrganizationMembers.Add(new OrganizationMember
            {
                OrganizationId = organizationId,
                UserId = user.Id,
                Role = role
            });
            await db.SaveChangesAsync(ct);

            return await LoadAsync(user.Id, ct);
        }, ct);

    public Task<Result<UserRecord>> SetRoleAsync(int organizationId, string id, OrganizationRole role, CancellationToken ct) =>
        InTransactionAsync(() => WithMemberAsync(organizationId, id, async (user, membership) =>
        {
            membership.Role = role;
            await db.SaveChangesAsync(ct);
            return await users.UpdateSecurityStampAsync(user);
        }, "Role", ct), ct);

    public Task<Result<UserRecord>> SetActiveAsync(int organizationId, string id, bool isActive, CancellationToken ct) =>
        InTransactionAsync(() => WithMemberAsync(organizationId, id, async (user, _) =>
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

    public Task<Result<UserRecord>> ResetPasswordAsync(int organizationId, string id, string newPassword, CancellationToken ct) =>
        // The transaction restores the old password if the new one fails the strength rules.
        InTransactionAsync(() => WithMemberAsync(organizationId, id, async (user, _) =>
        {
            var removed = await users.RemovePasswordAsync(user);
            return removed.Succeeded ? await users.AddPasswordAsync(user, newPassword) : removed;
        }, "NewPassword", ct), ct);

    public async Task<Result<UserRecord>> ChangePasswordAsync(
        string id, string currentPassword, string newPassword, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null)
        {
            return Result<UserRecord>.NotFound();
        }

        var result = await users.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded ? await LoadAsync(user.Id, ct) : ToFailure(result, "NewPassword");
    }

    public Task<Result<Unit>> RunWithAdminGuardAsync(Func<Task<Result<Unit>>> action, CancellationToken ct) =>
        InTransactionAsync(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(AdminGuardSql, ct);
            return await action();
        }, ct);

    private async Task<Result<UserRecord>> WithMemberAsync(
        int organizationId,
        string id,
        Func<IdentityUser, OrganizationMember, Task<IdentityResult>> change,
        string errorField,
        CancellationToken ct)
    {
        var membership = await db.OrganizationMembers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == id, ct);

        var user = membership is null ? null : await users.FindByIdAsync(membership.UserId);
        if (membership is null || user is null)
        {
            return Result<UserRecord>.NotFound();
        }

        var result = await change(user, membership);
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

    private IQueryable<UserRow> QueryRows()
    {
        var memberships = db.OrganizationMembers.IgnoreQueryFilters();

        return db.Users
            .AsNoTracking()
            .Where(u => u.Id != SeedIds.SystemUserId)
            .Select(u => new UserRow
            {
                Id = u.Id,
                Email = u.Email,
                LockoutEnd = u.LockoutEnd,
                SecurityStamp = u.SecurityStamp,
                OrganizationId = memberships
                    .Where(m => m.UserId == u.Id)
                    .Select(m => (int?)m.OrganizationId)
                    .FirstOrDefault(),
                OrganizationName = memberships
                    .Where(m => m.UserId == u.Id)
                    .Select(m => m.Organization.Name)
                    .FirstOrDefault(),
                OrganizationRole = memberships
                    .Where(m => m.UserId == u.Id)
                    .Select(m => (OrganizationRole?)m.Role)
                    .FirstOrDefault(),
                OrganizationIsActive = memberships
                    .Where(m => m.UserId == u.Id)
                    .Select(m => (bool?)m.Organization.IsActive)
                    .FirstOrDefault(),
                IsSuperAdmin = (from userRole in db.UserRoles
                                join role in db.Roles on userRole.RoleId equals role.Id
                                where userRole.UserId == u.Id && role.Name == TokenClaims.SuperAdminRole
                                select role.Id).Any()
            });
    }

    private UserRecord ToRecord(UserRow row) => new(
        row.Id,
        row.Email ?? string.Empty,
        row.OrganizationId,
        row.OrganizationName,
        row.OrganizationRole,
        row.IsSuperAdmin,
        row.LockoutEnd is null || row.LockoutEnd <= timeProvider.GetUtcNow(),
        row.OrganizationIsActive == true,
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
        public int? OrganizationId { get; init; }
        public string? OrganizationName { get; init; }
        public OrganizationRole? OrganizationRole { get; init; }
        public bool? OrganizationIsActive { get; init; }
        public bool IsSuperAdmin { get; init; }
    }
}
