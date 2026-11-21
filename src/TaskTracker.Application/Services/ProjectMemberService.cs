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

public class ProjectMemberService(
    IAppDbContext db,
    IUserDirectory users,
    ICurrentUser currentUser,
    IProjectAccess access,
    IValidator<AddProjectMemberRequest> addValidator,
    IValidator<UpdateProjectMemberRequest> updateValidator,
    IValidator<TransferProjectOwnerRequest> transferValidator,
    TimeProvider timeProvider) : IProjectMemberService
{
    private const string InvalidUserMessage = "The user must be active and in this organization.";

    private int OrganizationId => currentUser.OrganizationId
        ?? throw new InvalidOperationException("Project membership requires an organization.");

    public async Task<Result<IReadOnlyList<ProjectMemberDto>>> GetMembersAsync(int projectId, CancellationToken ct)
    {
        if (await access.GetAsync(projectId, ct) is null)
        {
            return Result<IReadOnlyList<ProjectMemberDto>>.NotFound();
        }

        var members = await db.ProjectMembers
            .AsNoTracking()
            .Where(m => m.ProjectId == projectId)
            .ToListAsync(ct);

        var emails = await users.GetEmailsAsync(members.Select(m => m.UserId), ct);

        IReadOnlyList<ProjectMemberDto> dtos = members
            .Select(m => new ProjectMemberDto(m.UserId, emails.GetValueOrDefault(m.UserId, string.Empty), m.Role.ToContract()))
            .OrderBy(m => m.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result<IReadOnlyList<ProjectMemberDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<ProjectCandidateDto>>> GetCandidatesAsync(int projectId, CancellationToken ct)
    {
        var permissions = await access.GetAsync(projectId, ct);
        if (permissions is null)
        {
            return Result<IReadOnlyList<ProjectCandidateDto>>.NotFound();
        }

        if (!permissions.CanManageMembers && !permissions.CanTransferOwner)
        {
            return Result<IReadOnlyList<ProjectCandidateDto>>.Forbidden();
        }

        var organizationUsers = await users.GetInOrganizationAsync(OrganizationId, ct);

        IReadOnlyList<ProjectCandidateDto> candidates = organizationUsers
            .Where(u => u.IsActive)
            .Select(u => new ProjectCandidateDto(u.Id, u.Email))
            .ToList();

        return Result<IReadOnlyList<ProjectCandidateDto>>.Success(candidates);
    }

    public async Task<Result<ProjectMemberDto>> AddAsync(int projectId, AddProjectMemberRequest request, CancellationToken ct)
    {
        var validation = await addValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<ProjectMemberDto>.ValidationFailed(validation.ToDictionary());
        }

        var denied = await DenyMemberChangeAsync<ProjectMemberDto>(projectId, ct);
        if (denied is not null)
        {
            return denied;
        }

        var user = await users.GetInOrganizationAsync(OrganizationId, request.UserId, ct);
        if (user is not { IsActive: true })
        {
            return Result<ProjectMemberDto>.Invalid(nameof(request.UserId), InvalidUserMessage);
        }

        var ownerId = await db.Projects.Where(p => p.Id == projectId).Select(p => p.OwnerUserId).FirstOrDefaultAsync(ct);
        if (ownerId == user.Id)
        {
            return Result<ProjectMemberDto>.Conflict("The project owner already has full access.");
        }

        if (await db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == user.Id, ct))
        {
            return Result<ProjectMemberDto>.Conflict("This user is already a project member.");
        }

        var role = request.Role.ToDomain();
        db.ProjectMembers.Add(new ProjectMember
        {
            OrganizationId = OrganizationId,
            ProjectId = projectId,
            UserId = user.Id,
            Role = role,
            AddedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two simultaneous requests can both pass the check and hit the primary key.
            return Result<ProjectMemberDto>.Conflict("This user is already a project member.");
        }

        return Result<ProjectMemberDto>.Success(new ProjectMemberDto(user.Id, user.Email, role.ToContract()));
    }

    public async Task<Result<Unit>> UpdateAsync(
        int projectId, string userId, UpdateProjectMemberRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailed(validation.ToDictionary());
        }

        var denied = await DenyMemberChangeAsync<Unit>(projectId, ct);
        if (denied is not null)
        {
            return denied;
        }

        var member = await db.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId, ct);
        if (member is null)
        {
            return Result<Unit>.NotFound();
        }

        member.Role = request.Role.ToDomain();
        await db.SaveChangesAsync(ct);

        return Result<Unit>.Success(Unit.Value);
    }

    public async Task<Result<Unit>> RemoveAsync(int projectId, string userId, CancellationToken ct)
    {
        var denied = await DenyMemberChangeAsync<Unit>(projectId, ct);
        if (denied is not null)
        {
            return denied;
        }

        var member = await db.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId, ct);
        if (member is null)
        {
            return Result<Unit>.NotFound();
        }

        db.ProjectMembers.Remove(member);
        await db.SaveChangesAsync(ct);

        return Result<Unit>.Success(Unit.Value);
    }

    public async Task<Result<Unit>> TransferOwnerAsync(
        int projectId, TransferProjectOwnerRequest request, CancellationToken ct)
    {
        var validation = await transferValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailed(validation.ToDictionary());
        }

        var permissions = await access.GetAsync(projectId, ct);
        if (permissions is null)
        {
            return Result<Unit>.NotFound();
        }

        if (!permissions.CanTransferOwner)
        {
            return Result<Unit>.Forbidden();
        }

        var organizationId = OrganizationId;

        return await users.RunWithAdminGuardAsync(async () =>
        {
            var newOwner = await users.GetInOrganizationAsync(organizationId, request.OwnerUserId, ct);
            if (newOwner is not { IsActive: true })
            {
                return Result<Unit>.Invalid(nameof(request.OwnerUserId), InvalidUserMessage);
            }

            var project = await db.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId, ct);
            if (project is null)
            {
                return Result<Unit>.NotFound();
            }

            if (project.OwnerUserId == newOwner.Id)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            var previousOwnerId = project.OwnerUserId;

            // The new owner's access now comes from ownership, so a member row would be redundant.
            var existing = project.Members.FirstOrDefault(m => m.UserId == newOwner.Id);
            if (existing is not null)
            {
                db.ProjectMembers.Remove(existing);
            }

            project.OwnerUserId = newOwner.Id;

            // The previous owner keeps working on the project. The seeded system owner is not a real user.
            if (await users.GetInOrganizationAsync(organizationId, previousOwnerId, ct) is not null)
            {
                db.ProjectMembers.Add(new ProjectMember
                {
                    OrganizationId = organizationId,
                    ProjectId = projectId,
                    UserId = previousOwnerId,
                    Role = DomainRole.Contributor,
                    AddedAtUtc = timeProvider.GetUtcNow().UtcDateTime
                });
            }

            await db.SaveChangesAsync(ct);
            return Result<Unit>.Success(Unit.Value);
        }, ct);
    }

    private async Task<Result<T>?> DenyMemberChangeAsync<T>(int projectId, CancellationToken ct)
    {
        var permissions = await access.GetAsync(projectId, ct);

        if (permissions is null)
        {
            return Result<T>.NotFound();
        }

        return permissions.CanManageMembers ? null : Result<T>.Forbidden();
    }
}
