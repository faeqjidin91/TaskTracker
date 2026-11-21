using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Abstractions;
using TaskTracker.Domain.Entities;
using TaskTracker.Infrastructure.Notifications;

namespace TaskTracker.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : IdentityDbContext<IdentityUser>(options), IAppDbContext
{
    // Read when each query runs. With no organization, the filters below match no rows.
    public int? CurrentOrganizationId => currentUser.OrganizationId;

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    // Not on IAppDbContext: this table is an Infrastructure detail of one notification provider.
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Tenant isolation lives here so a service cannot forget it.
        builder.Entity<OrganizationMember>().HasQueryFilter(m => m.OrganizationId == CurrentOrganizationId);
        builder.Entity<Project>().HasQueryFilter(p => p.OrganizationId == CurrentOrganizationId);
        builder.Entity<ProjectMember>().HasQueryFilter(m => m.OrganizationId == CurrentOrganizationId);
        builder.Entity<TaskItem>().HasQueryFilter(t => t.Project.OrganizationId == CurrentOrganizationId);
    }
}
