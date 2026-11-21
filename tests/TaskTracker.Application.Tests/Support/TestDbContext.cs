using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Abstractions;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Tests.Support;

// The test project cannot reference Infrastructure, so this mirrors AppDbContext's tenant
// filters. These tests prove services work correctly behind the filters. They do not prove
// that the production AppDbContext declares them.
public sealed class TestDbContext(DbContextOptions<TestDbContext> options, ICurrentUser currentUser)
    : DbContext(options), IAppDbContext
{
    public int? CurrentOrganizationId => currentUser.OrganizationId;
    public bool FailNextSave { get; set; }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (FailNextSave)
        {
            FailNextSave = false;
            throw new InvalidOperationException("Simulated save failure.");
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<OrganizationMember>().HasKey(m => new { m.OrganizationId, m.UserId });
        builder.Entity<ProjectMember>().HasKey(m => new { m.ProjectId, m.UserId });

        builder.Entity<OrganizationMember>().HasQueryFilter(m => m.OrganizationId == CurrentOrganizationId);
        builder.Entity<Project>().HasQueryFilter(p => p.OrganizationId == CurrentOrganizationId);
        builder.Entity<ProjectMember>().HasQueryFilter(m => m.OrganizationId == CurrentOrganizationId);
        builder.Entity<TaskItem>().HasQueryFilter(t => t.Project.OrganizationId == CurrentOrganizationId);
    }
}
