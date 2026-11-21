using TaskTracker.Application.Common;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Abstractions;

public record UserRecord(string Id, string Email, UserRole Role, bool IsActive, string SecurityStamp);

// Keeps ASP.NET Core Identity out of the Application layer.
public interface IUserDirectory
{
    Task<IReadOnlyList<UserRecord>> GetAllAsync(CancellationToken ct);
    Task<UserRecord?> GetByIdAsync(string id, CancellationToken ct);
    Task<int> CountActiveAdminsAsync(CancellationToken ct);
    Task<Result<UserRecord>> CreateAsync(string email, string password, UserRole role, CancellationToken ct);
    Task<Result<UserRecord>> SetRoleAsync(string id, UserRole role, CancellationToken ct);
    Task<Result<UserRecord>> SetActiveAsync(string id, bool isActive, CancellationToken ct);
    Task<Result<UserRecord>> ResetPasswordAsync(string id, string newPassword, CancellationToken ct);
    Task<Result<UserRecord>> ChangePasswordAsync(string id, string currentPassword, string newPassword, CancellationToken ct);

    // Runs one admin-affecting change at a time, in a transaction that commits only on success.
    // Without this, two admins demoting each other at once could leave the system with none.
    Task<Result<Unit>> RunWithAdminGuardAsync(Func<Task<Result<Unit>>> action, CancellationToken ct);
}
