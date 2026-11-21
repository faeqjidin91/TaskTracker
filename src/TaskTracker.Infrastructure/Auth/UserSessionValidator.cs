using Microsoft.EntityFrameworkCore;
using TaskTracker.Infrastructure.Persistence;

namespace TaskTracker.Infrastructure.Auth;

// JWTs are otherwise valid until they expire. Role, status, and password changes rotate the
// security stamp, so this check makes those changes apply to existing tokens immediately.
public sealed class UserSessionValidator(AppDbContext db, TimeProvider timeProvider)
{
    public async Task<bool> IsCurrentAsync(string? userId, string? stampHash, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(stampHash))
        {
            return false;
        }

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.SecurityStamp, u.LockoutEnd })
            .FirstOrDefaultAsync(ct);

        return user is { SecurityStamp: not null }
            && (user.LockoutEnd is null || user.LockoutEnd <= timeProvider.GetUtcNow())
            && TokenClaims.HashStamp(user.SecurityStamp) == stampHash;
    }
}
