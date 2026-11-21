using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Abstractions;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Authorization;

public interface IProjectAccess
{
    /// <returns>The current user's permissions, or null when the project is not visible to them.</returns>
    Task<ProjectPermissions?> GetAsync(int projectId, CancellationToken ct);
}

public sealed class ProjectAccess(IAppDbContext db, ICurrentUser currentUser) : IProjectAccess
{
    public async Task<ProjectPermissions?> GetAsync(int projectId, CancellationToken ct)
    {
        if (currentUser.Id is not { } userId)
        {
            return null;
        }

        var project = await db.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new
            {
                p.OwnerUserId,
                MemberRole = p.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (ProjectRole?)m.Role)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        return project is null
            ? null
            : ProjectPermissions.For(currentUser.IsOrganizationAdmin, project.OwnerUserId == userId, project.MemberRole);
    }
}
