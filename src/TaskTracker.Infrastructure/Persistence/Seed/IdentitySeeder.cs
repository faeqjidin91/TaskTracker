using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Infrastructure.Auth;

namespace TaskTracker.Infrastructure.Persistence.Seed;

public class IdentitySeeder(UserManager<IdentityUser> userManager, AppDbContext db, ILogger<IdentitySeeder> logger)
{
    private const string TestAdminEmail = "admin@tasktracker.local";
    private const string TestSuperAdminEmail = "superadmin@tasktracker.local";
    private const string TestPassword = "Passw0rd!";

    // Development only: documented local accounts, with the demo projects owned by the Admin.
    public async Task SeedDevelopmentAsync(CancellationToken ct)
    {
        await EnsureSuperAdminAsync(TestSuperAdminEmail, TestPassword, ct);

        var admin = await userManager.FindByEmailAsync(TestAdminEmail);
        if (admin is null)
        {
            admin = new IdentityUser { UserName = TestAdminEmail, Email = TestAdminEmail, EmailConfirmed = true };
            EnsureSucceeded(await userManager.CreateAsync(admin, TestPassword));
            logger.LogInformation("Created test user {Email}", TestAdminEmail);
        }

        var memberships = db.OrganizationMembers.IgnoreQueryFilters();
        if (!await memberships.AnyAsync(m => m.UserId == admin.Id, ct))
        {
            db.OrganizationMembers.Add(new OrganizationMember
            {
                OrganizationId = SeedIds.DefaultOrganizationId,
                UserId = admin.Id,
                Role = OrganizationRole.Admin
            });
            await db.SaveChangesAsync(ct);
        }

        var demoProjects = await db.Projects
            .IgnoreQueryFilters()
            .Where(p => p.OwnerUserId == SeedIds.SystemUserId)
            .ToListAsync(ct);

        foreach (var project in demoProjects)
        {
            project.OwnerUserId = admin.Id;
        }

        await db.SaveChangesAsync(ct);
    }

    // Any environment: creates the first SuperAdmin from secrets, and only if none exists.
    public async Task BootstrapSuperAdminAsync(string email, string password, CancellationToken ct)
    {
        if ((await userManager.GetUsersInRoleAsync(TokenClaims.SuperAdminRole)).Count > 0)
        {
            return;
        }

        await EnsureSuperAdminAsync(email, password, ct);
    }

    private async Task EnsureSuperAdminAsync(string email, string password, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            EnsureSucceeded(await userManager.CreateAsync(user, password));
            logger.LogInformation("Created SuperAdmin {Email}", email);
        }
        else if (await db.OrganizationMembers.IgnoreQueryFilters().AnyAsync(m => m.UserId == user.Id, ct))
        {
            throw new InvalidOperationException($"{email} belongs to an organization and cannot be a SuperAdmin.");
        }

        if (!await userManager.IsInRoleAsync(user, TokenClaims.SuperAdminRole))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, TokenClaims.SuperAdminRole));
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not seed user: {errors}");
        }
    }
}
