using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Abstractions;

// Every set except Organizations is filtered to the signed-in user's organization.
public interface IAppDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<OrganizationMember> OrganizationMembers { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<TaskItem> Tasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
