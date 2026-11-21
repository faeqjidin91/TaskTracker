using TaskTracker.Application.Common;
using TaskTracker.Contracts.Projects;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Mapping;

public static class ProjectMappings
{
    // The count is passed in so callers can get it from SQL without loading every task.
    public static ProjectDto ToDto(this Project project, int taskCount) => new(
        project.Id,
        project.Name,
        project.Description,
        taskCount,
        project.CreatedAtUtc.AsUtc());
}
