using Microsoft.AspNetCore.Identity;
using TaskTracker.Application.Abstractions;

namespace TaskTracker.Infrastructure.Auth;

public sealed class IdentityUserAuthenticator(UserManager<IdentityUser> users, IUserDirectory directory)
    : IUserAuthenticator
{
    public async Task<UserRecord?> AuthenticateAsync(string email, string password, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = await users.FindByEmailAsync(email);
        if (user is null || !await users.CheckPasswordAsync(user, password))
        {
            return null;
        }

        var record = await directory.GetByIdAsync(user.Id, ct);
        return record is { IsActive: true } ? record : null;
    }
}
