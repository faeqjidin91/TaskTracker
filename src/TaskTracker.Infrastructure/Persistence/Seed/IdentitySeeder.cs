using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace TaskTracker.Infrastructure.Persistence.Seed;

// Creates the documented local test user. The Api runs this in Development only.
public class IdentitySeeder(UserManager<IdentityUser> userManager, ILogger<IdentitySeeder> logger)
{
    private const string TestEmail = "admin@tasktracker.local";
    private const string TestPassword = "Passw0rd!";

    public async Task SeedAsync()
    {
        if (await userManager.FindByEmailAsync(TestEmail) is not null)
        {
            return;
        }

        var user = new IdentityUser { UserName = TestEmail, Email = TestEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, TestPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create test user: {errors}");
        }

        logger.LogInformation("Created test user {Email}", TestEmail);
    }
}
