using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Infrastructure.Auth;

namespace TaskTracker.Infrastructure.Persistence.Seed;

public class IdentitySeeder(UserManager<IdentityUser> userManager, AppDbContext db, ILogger<IdentitySeeder> logger)
{
    private static readonly DateTime DemoDate = new(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);

    // Development only: the SuperAdmin, organization, Admin, and demo projects from .env.
    public async Task SeedDevelopmentAsync(SeedOptions seed, CancellationToken ct)
    {
        seed.EnsureValidForDevelopment();
        await EnsureSuperAdminAsync(seed.SuperAdminEmail!, seed.SuperAdminPassword!, ct);

        var admin = await userManager.FindByEmailAsync(seed.AdminEmail!);
        if (admin is not null)
        {
            var existing = await db.OrganizationMembers
                .IgnoreQueryFilters()
                .Include(m => m.Organization)
                .FirstOrDefaultAsync(m => m.UserId == admin.Id, ct);

            if (existing is not null)
            {
                if (!string.Equals(existing.Organization.Name, seed.OrganizationName, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "Seed admin {Email} already belongs to {Organization}, so {Configured} was not seeded",
                        seed.AdminEmail, existing.Organization.Name, seed.OrganizationName);
                }

                return;
            }
        }

        // One transaction, so a rejected Admin password never leaves an empty organization.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var organization = await db.Organizations.FirstOrDefaultAsync(o => o.Name == seed.OrganizationName, ct);
        var isNewOrganization = organization is null;
        if (organization is null)
        {
            organization = new Organization { Name = seed.OrganizationName!.Trim(), CreatedAtUtc = DateTime.UtcNow };
            db.Organizations.Add(organization);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Created organization {Organization}", organization.Name);
        }

        if (admin is null)
        {
            admin = NewUser(seed.AdminEmail!);
            EnsureSucceeded(await userManager.CreateAsync(admin, seed.AdminPassword!));
            logger.LogInformation("Created Admin {Email}", seed.AdminEmail);
        }

        db.OrganizationMembers.Add(new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = admin.Id,
            Role = OrganizationRole.Admin
        });
        await db.SaveChangesAsync(ct);

        // Only for a new organization, so deleted demo projects do not come back on restart.
        if (isNewOrganization)
        {
            await AddDemoProjectsAsync(organization.Id, admin.Id, ct);
        }

        await transaction.CommitAsync(ct);
    }

    // Deployment: creates the first SuperAdmin only if none exists. Nothing else is seeded.
    public async Task SeedDeploymentAsync(SeedOptions seed, CancellationToken ct)
    {
        if (!seed.EnsureValidForDeployment())
        {
            return;
        }

        if ((await userManager.GetUsersInRoleAsync(TokenClaims.SuperAdminRole)).Count > 0)
        {
            return;
        }

        await EnsureSuperAdminAsync(seed.SuperAdminEmail!, seed.SuperAdminPassword!, ct);
    }

    private async Task EnsureSuperAdminAsync(string email, string password, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = NewUser(email);
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

    private async Task AddDemoProjectsAsync(int organizationId, string ownerId, CancellationToken ct)
    {
        Project[] projects =
        [
            DemoProject(organizationId, ownerId, "Website Redesign", "Refresh the marketing site with a new design system.",
                DemoTask("Audit existing pages", TaskItemStatus.Done, TaskPriority.Medium, null),
                DemoTask("Build new landing page", TaskItemStatus.InProgress, TaskPriority.High, new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc)),
                DemoTask("Migrate blog content", TaskItemStatus.Todo, TaskPriority.Low, null)),
            DemoProject(organizationId, ownerId, "Mobile App v2", "Second major release of the mobile app.",
                DemoTask("Design onboarding flow", TaskItemStatus.InProgress, TaskPriority.Medium, new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc)),
                DemoTask("Add push notifications", TaskItemStatus.Todo, TaskPriority.High, null))
        ];

        foreach (var project in projects)
        {
            db.Projects.Add(project);
            await db.SaveChangesAsync(ct);
        }
    }

    private static Project DemoProject(
        int organizationId, string ownerId, string name, string description, params TaskItem[] tasks) => new()
    {
        OrganizationId = organizationId,
        OwnerUserId = ownerId,
        Name = name,
        Description = description,
        CreatedAtUtc = DemoDate,
        Tasks = tasks
    };

    private static TaskItem DemoTask(string title, TaskItemStatus status, TaskPriority priority, DateTime? dueDateUtc) => new()
    {
        Title = title,
        Status = status,
        Priority = priority,
        DueDateUtc = dueDateUtc,
        CreatedAtUtc = DemoDate,
        UpdatedAtUtc = DemoDate
    };

    private static IdentityUser NewUser(string email) => new() { UserName = email, Email = email, EmailConfirmed = true };

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not seed user: {errors}");
        }
    }
}
