using Microsoft.AspNetCore.Identity;
using TaskTracker.Application.Abstractions;

namespace TaskTracker.Infrastructure.Auth;

public sealed class IdentityUserAuthenticator(UserManager<IdentityUser> users) : IUserAuthenticator
{
    public async Task<AuthenticatedUser?> AuthenticateAsync(string email, string password, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = await users.FindByEmailAsync(email);
        if (user is null || !await users.CheckPasswordAsync(user, password))
        {
            return null;
        }

        return new AuthenticatedUser(user.Id, user.Email!);
    }
}
