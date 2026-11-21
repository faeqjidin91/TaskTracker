using TaskTracker.Application.Common;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Abstractions;

public record UserRecord(
    string Id,
    string Email,
    int? OrganizationId,
    string? OrganizationName,
    OrganizationRole? OrganizationRole,
    bool IsSuperAdmin,
    bool IsActive,
    bool OrganizationIsActive,
    string SecurityStamp)
{
    public bool CanSignIn => IsActive && (IsSuperAdmin || (OrganizationId is not null && OrganizationIsActive));
}

// Keeps ASP.NET Core Identity out of the Application layer.
public interface IUserDirectory
{
    Task<UserRecord?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<UserRecord>> GetInOrganizationAsync(int organizationId, CancellationToken ct);
    Task<UserRecord?> GetInOrganizationAsync(int organizationId, string id, CancellationToken ct);
    Task<IReadOnlyDictionary<string, string>> GetEmailsAsync(IEnumerable<string> ids, CancellationToken ct);
    Task<IReadOnlyDictionary<int, int>> GetOrganizationUserCountsAsync(CancellationToken ct);
    Task<int> CountActiveAdminsAsync(int organizationId, CancellationToken ct);

    Task<Result<UserRecord>> CreateAsync(
        int organizationId, string email, string password, OrganizationRole role, CancellationToken ct);

    Task<Result<UserRecord>> SetRoleAsync(int organizationId, string id, OrganizationRole role, CancellationToken ct);
    Task<Result<UserRecord>> SetActiveAsync(int organizationId, string id, bool isActive, CancellationToken ct);
    Task<Result<UserRecord>> ResetPasswordAsync(int organizationId, string id, string newPassword, CancellationToken ct);
    Task<Result<UserRecord>> ChangePasswordAsync(string id, string currentPassword, string newPassword, CancellationToken ct);

    // Runs one membership-affecting change at a time, in a transaction that commits only on
    // success. This prevents races such as two Admins demoting each other, or a user being
    // deactivated at the moment they are made a project owner.
    Task<Result<Unit>> RunWithAdminGuardAsync(Func<Task<Result<Unit>>> action, CancellationToken ct);
}
