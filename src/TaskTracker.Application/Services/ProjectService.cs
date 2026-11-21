using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Common;
using TaskTracker.Application.Mapping;
using TaskTracker.Contracts.Projects;
using TaskTracker.Domain.Entities;
using DomainRole = TaskTracker.Domain.Enums.ProjectRole;

namespace TaskTracker.Application.Services;

public class ProjectService(
    IAppDbContext db,
    IUserDirectory users,
    ICurrentUser currentUser,
    IProjectAccess access,
    IValidator<CreateProjectRequest> createValidator,
    IValidator<UpdateProjectRequest> updateValidator,
    TimeProvider timeProvider) : IProjectService
{
    public async Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(CancellationToken ct)
    {
        var userId = currentUser.Id ?? string.Empty;
        var isAdmin = currentUser.IsOrganizationAdmin;

        var rows = await db.Projects
            .AsNoTracking()
            .Where(p => isAdmin || p.OwnerUserId == userId || p.Members.Any(m => m.UserId == userId))
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                Project = p,
                TaskCount = p.Tasks.Count,
                MemberRole = p.Members.Where(m => m.UserId == userId).Select(m => (DomainRole?)m.Role).FirstOrDefault()
            })
            .ToListAsync(ct);

        var emails = await users.GetEmailsAsync(rows.Select(r => r.Project.OwnerUserId).Distinct(), ct);

        IReadOnlyList<ProjectDto> projects = rows
            .Select(r => r.Project.ToDto(
                r.TaskCount,
                emails.GetValueOrDefault(r.Project.OwnerUserId, string.Empty),
                ProjectPermissions.For(isAdmin, r.Project.OwnerUserId == userId, r.MemberRole)!))
            .ToList();

        return Result<IReadOnlyList<ProjectDto>>.Success(projects);
    }

    public async Task<Result<ProjectDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var permissions = await access.GetAsync(id, ct);
        if (permissions is null)
        {
            return Result<ProjectDto>.NotFound();
        }

        var row = await db.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { Project = p, TaskCount = p.Tasks.Count })
            .FirstOrDefaultAsync(ct);

        if (row is null)
        {
            return Result<ProjectDto>.NotFound();
        }

        var emails = await users.GetEmailsAsync([row.Project.OwnerUserId], ct);
        var ownerEmail = emails.GetValueOrDefault(row.Project.OwnerUserId, string.Empty);

        return Result<ProjectDto>.Success(row.Project.ToDto(row.TaskCount, ownerEmail, permissions));
    }

    public async Task<Result<ProjectDto>> CreateAsync(CreateProjectRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<ProjectDto>.ValidationFailed(validation.ToDictionary());
        }

        if (!currentUser.IsOrganizationAdmin
            || currentUser.OrganizationId is not { } organizationId
            || currentUser.Id is not { } currentUserId)
        {
            return Result<ProjectDto>.Forbidden();
        }

        var ownerId = string.IsNullOrWhiteSpace(request.OwnerUserId) ? currentUserId : request.OwnerUserId;
        Project? project = null;
        string ownerEmail = string.Empty;

        var saved = await users.RunWithAdminGuardAsync(async () =>
        {
            var owner = await users.GetInOrganizationAsync(organizationId, ownerId, ct);
            if (owner is not { IsActive: true })
            {
                return Result<Unit>.Invalid(nameof(request.OwnerUserId), "The owner must be an active user in this organization.");
            }

            ownerEmail = owner.Email;
            project = new Project
            {
                OrganizationId = organizationId,
                OwnerUserId = owner.Id,
                Name = request.Name.Trim(),
                Description = request.Description,
                CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
            };

            db.Projects.Add(project);
            await db.SaveChangesAsync(ct);
            return Result<Unit>.Success(Unit.Value);
        }, ct);

        if (!saved.IsSuccess)
        {
            return saved.ToFailure<ProjectDto>();
        }

        var permissions = ProjectPermissions.For(true, project!.OwnerUserId == currentUserId, null)!;
        return Result<ProjectDto>.Success(project.ToDto(taskCount: 0, ownerEmail, permissions));
    }

    public async Task<Result<Unit>> UpdateAsync(int id, UpdateProjectRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailed(validation.ToDictionary());
        }

        var permissions = await access.GetAsync(id, ct);
        if (permissions is null)
        {
            return Result<Unit>.NotFound();
        }

        if (!permissions.CanEdit)
        {
            return Result<Unit>.Forbidden();
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
        var permissions = await access.GetAsync(id, ct);
        if (permissions is null)
        {
            return Result<Unit>.NotFound();
        }

        if (!permissions.CanDelete)
        {
            return Result<Unit>.Forbidden();
        }

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
