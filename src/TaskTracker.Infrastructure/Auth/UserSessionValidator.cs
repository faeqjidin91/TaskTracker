using Microsoft.EntityFrameworkCore;
using TaskTracker.Infrastructure.Persistence;

namespace TaskTracker.Infrastructure.Auth;

// JWTs are otherwise valid until they expire. Role, status, and password changes rotate the
// security stamp, and organizations have an active flag, so this check applies those changes
// to existing tokens immediately.
public sealed class UserSessionValidator(AppDbContext db, TimeProvider timeProvider)
{
    public async Task<bool> IsCurrentAsync(
        string? userId, string? stampHash, bool tokenHasOrganization, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(stampHash))
        {
            return false;
        }

        var memberships = db.OrganizationMembers.IgnoreQueryFilters();

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.SecurityStamp,
                u.LockoutEnd,
                OrganizationIsActive = memberships
                    .Where(m => m.UserId == u.Id)
                    .Select(m => (bool?)m.Organization.IsActive)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        return user is { SecurityStamp: not null }
            && (user.LockoutEnd is null || user.LockoutEnd <= timeProvider.GetUtcNow())
            && (!tokenHasOrganization || user.OrganizationIsActive == true)
            && TokenClaims.HashStamp(user.SecurityStamp) == stampHash;
    }
}
