using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Project> Projects { get; }
    DbSet<TaskItem> Tasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
