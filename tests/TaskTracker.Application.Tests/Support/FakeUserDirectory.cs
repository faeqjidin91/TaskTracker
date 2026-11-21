using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Common;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Tests.Support;

// An in-memory stand-in for the ASP.NET Core Identity implementation.
public sealed class FakeUserDirectory : IUserDirectory
{
    public const string WeakPassword = "weak";

    private readonly List<UserRecord> _users = [];

    public int GuardRuns { get; private set; }

    public UserRecord Add(string id, int? organizationId, OrganizationRole? role, bool isSuperAdmin = false)
    {
        var user = new UserRecord(
            id, $"{id}@test.local", organizationId, organizationId is null ? null : $"Org {organizationId}",
            role, isSuperAdmin, IsActive: true, OrganizationIsActive: organizationId is not null, SecurityStamp: id);

        _users.Add(user);
        return user;
    }

    public UserRecord Get(string id) => _users.Single(u => Matches(u, id));

    public void Update(string id, Func<UserRecord, UserRecord> change)
    {
        var index = _users.FindIndex(u => Matches(u, id));
        _users[index] = change(_users[index]);
    }

    public Task<UserRecord?> GetByIdAsync(string id, CancellationToken ct) =>
        Task.FromResult(_users.FirstOrDefault(u => Matches(u, id)));

    public Task<IReadOnlyList<UserRecord>> GetInOrganizationAsync(int organizationId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UserRecord>>(_users.Where(u => u.OrganizationId == organizationId).ToList());

    public Task<UserRecord?> GetInOrganizationAsync(int organizationId, string id, CancellationToken ct) =>
        Task.FromResult(_users.FirstOrDefault(u => Matches(u, id) && u.OrganizationId == organizationId));

    public Task<IReadOnlyDictionary<string, string>> GetEmailsAsync(IEnumerable<string> ids, CancellationToken ct)
    {
        var wanted = ids.ToHashSet();
        return Task.FromResult<IReadOnlyDictionary<string, string>>(
            _users.Where(u => wanted.Contains(u.Id)).ToDictionary(u => u.Id, u => u.Email));
    }

    public Task<IReadOnlyDictionary<int, int>> GetOrganizationUserCountsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<int, int>>(_users
            .Where(u => u.OrganizationId is not null)
            .GroupBy(u => u.OrganizationId!.Value)
            .ToDictionary(group => group.Key, group => group.Count()));

    public Task<int> CountActiveAdminsAsync(int organizationId, CancellationToken ct) =>
        Task.FromResult(_users.Count(u =>
            u.OrganizationId == organizationId && u.OrganizationRole == OrganizationRole.Admin && u.IsActive));

    public Task<Result<UserRecord>> CreateAsync(
        int organizationId, string email, string password, OrganizationRole role, CancellationToken ct)
    {
        if (password == WeakPassword)
        {
            return Task.FromResult(Result<UserRecord>.Invalid("Password", "Password is too weak."));
        }

        if (_users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult(Result<UserRecord>.Conflict("A user with this email already exists."));
        }

        var user = new UserRecord(
            Guid.NewGuid().ToString(), email, organizationId, $"Org {organizationId}",
            role, IsSuperAdmin: false, IsActive: true, OrganizationIsActive: true, SecurityStamp: "new");

        _users.Add(user);
        return Task.FromResult(Result<UserRecord>.Success(user));
    }

    public Task<Result<UserRecord>> SetRoleAsync(int organizationId, string id, OrganizationRole role, CancellationToken ct) =>
        Change(organizationId, id, user => user with { OrganizationRole = role });

    public Task<Result<UserRecord>> SetActiveAsync(int organizationId, string id, bool isActive, CancellationToken ct) =>
        Change(organizationId, id, user => user with { IsActive = isActive });

    public Task<Result<UserRecord>> ResetPasswordAsync(int organizationId, string id, string newPassword, CancellationToken ct) =>
        newPassword == WeakPassword
            ? Task.FromResult(Result<UserRecord>.Invalid("NewPassword", "Password is too weak."))
            : Change(organizationId, id, user => user with { SecurityStamp = Guid.NewGuid().ToString() });

    public Task<Result<UserRecord>> ChangePasswordAsync(
        string id, string currentPassword, string newPassword, CancellationToken ct) =>
        Task.FromResult(Result<UserRecord>.Success(Get(id)));

    public Task<Result<Unit>> RunWithAdminGuardAsync(Func<Task<Result<Unit>>> action, CancellationToken ct)
    {
        GuardRuns++;
        return action();
    }

    private Task<Result<UserRecord>> Change(int organizationId, string id, Func<UserRecord, UserRecord> change)
    {
        var index = _users.FindIndex(u => Matches(u, id) && u.OrganizationId == organizationId);
        if (index < 0)
        {
            return Task.FromResult(Result<UserRecord>.NotFound());
        }

        _users[index] = change(_users[index]);
        return Task.FromResult(Result<UserRecord>.Success(_users[index]));
    }

    // SQL Server compares ids case-insensitively, so the fake does too.
    private static bool Matches(UserRecord user, string id) =>
        string.Equals(user.Id, id, StringComparison.OrdinalIgnoreCase);
}
