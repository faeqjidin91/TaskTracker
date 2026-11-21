using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Common;
using TaskTracker.Application.Mapping;
using TaskTracker.Contracts.Projects;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Services;

public class ProjectService(
    IAppDbContext db,
    IValidator<CreateProjectRequest> createValidator,
    IValidator<UpdateProjectRequest> updateValidator,
    TimeProvider timeProvider) : IProjectService
{
    public async Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(CancellationToken ct)
    {
        var rows = await db.Projects
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new { Project = p, TaskCount = p.Tasks.Count })
            .ToListAsync(ct);

        IReadOnlyList<ProjectDto> projects = rows.Select(r => r.Project.ToDto(r.TaskCount)).ToList();
        return Result<IReadOnlyList<ProjectDto>>.Success(projects);
    }

    public async Task<Result<ProjectDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var row = await db.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { Project = p, TaskCount = p.Tasks.Count })
            .FirstOrDefaultAsync(ct);

        return row is null
            ? Result<ProjectDto>.NotFound()
            : Result<ProjectDto>.Success(row.Project.ToDto(row.TaskCount));
    }

    public async Task<Result<ProjectDto>> CreateAsync(CreateProjectRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<ProjectDto>.ValidationFailed(validation.ToDictionary());
        }

        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        return Result<ProjectDto>.Success(project.ToDto(taskCount: 0));
    }

    public async Task<Result<Unit>> UpdateAsync(int id, UpdateProjectRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailed(validation.ToDictionary());
        }

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
        {
            return Result<Unit>.NotFound();
        }

        project.Name = request.Name.Trim();
        project.Description = request.Description;
        await db.SaveChangesAsync(ct);

        return Result<Unit>.Success(Unit.Value);
    }

    public async Task<Result<Unit>> DeleteAsync(int id, CancellationToken ct)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
        {
            return Result<Unit>.NotFound();
        }

        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);

        return Result<Unit>.Success(Unit.Value);
    }
}
