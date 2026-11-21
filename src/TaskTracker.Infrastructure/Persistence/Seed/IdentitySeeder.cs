using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Infrastructure.Persistence.Seed;

// Creates the documented local test user as an Admin. The Api runs this in Development only.
public class IdentitySeeder(UserManager<IdentityUser> userManager, ILogger<IdentitySeeder> logger)
{
    private const string TestEmail = "admin@tasktracker.local";
    private const string TestPassword = "Passw0rd!";

    public async Task SeedAsync()
    {
        var user = await userManager.FindByEmailAsync(TestEmail);
        if (user is null)
        {
            user = new IdentityUser { UserName = TestEmail, Email = TestEmail, EmailConfirmed = true };
            EnsureSucceeded(await userManager.CreateAsync(user, TestPassword));
            logger.LogInformation("Created test user {Email}", TestEmail);
        }

        // Databases seeded before roles existed already have this user without a role.
        var admin = nameof(UserRole.Admin);
        if (!await userManager.IsInRoleAsync(user, admin))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, admin));
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not seed test user: {errors}");
        }
    }
}
