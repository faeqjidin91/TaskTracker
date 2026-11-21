using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Common;
using TaskTracker.Application.Mapping;
using TaskTracker.Contracts.Organizations;
using TaskTracker.Contracts.Users;
using TaskTracker.Domain.Entities;
using DomainRole = TaskTracker.Domain.Enums.OrganizationRole;

namespace TaskTracker.Application.Services;

// SuperAdmin operations. These manage organizations but never read their projects or tasks.
public class OrganizationService(
    IAppDbContext db,
    IUserDirectory users,
    IValidator<CreateOrganizationRequest> createValidator,
    IValidator<AddOrganizationAdminRequest> addAdminValidator,
    TimeProvider timeProvider) : IOrganizationService
{
    public async Task<Result<IReadOnlyList<OrganizationDto>>> GetAllAsync(CancellationToken ct)
    {
        var organizations = await db.Organizations.AsNoTracking().OrderBy(o => o.Name).ToListAsync(ct);
        var counts = await users.GetOrganizationUserCountsAsync(ct);

        IReadOnlyList<OrganizationDto> dtos = organizations
            .Select(o => ToDto(o, counts.GetValueOrDefault(o.Id)))
            .ToList();

        return Result<IReadOnlyList<OrganizationDto>>.Success(dtos);
    }

    public async Task<Result<OrganizationDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var organization = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
        if (organization is null)
        {
            return Result<OrganizationDto>.NotFound();
        }

        var counts = await users.GetOrganizationUserCountsAsync(ct);
        return Result<OrganizationDto>.Success(ToDto(organization, counts.GetValueOrDefault(id)));
    }

    public async Task<Result<OrganizationDto>> CreateAsync(CreateOrganizationRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<OrganizationDto>.ValidationFailed(validation.ToDictionary());
        }

        var name = request.Name.Trim();
        Organization? organization = null;

        // One transaction, so a rejected first Admin never leaves an organization behind.
        var saved = await users.RunWithAdminGuardAsync(async () =>
        {
            if (await db.Organizations.AnyAsync(o => o.Name == name, ct))
            {
                return Result<Unit>.Conflict("An organization with this name already exists.");
            }

            organization = new Organization
            {
                Name = name,
                CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
            };

            db.Organizations.Add(organization);
            await db.SaveChangesAsync(ct);

            var admin = await users.CreateAsync(
                organization.Id, request.AdminEmail.Trim(), request.AdminPassword, DomainRole.Admin, ct);

            return admin.IsSuccess ? Result<Unit>.Success(Unit.Value) : admin.ToFailure<Unit>();
        }, ct);

        return saved.IsSuccess
            ? Result<OrganizationDto>.Success(ToDto(organization!, userCount: 1))
            : saved.ToFailure<OrganizationDto>();
    }

    public async Task<Result<Unit>> UpdateStatusAsync(int id, UpdateOrganizationStatusRequest request, CancellationToken ct)
    {
        var organization = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (organization is null)
        {
            return Result<Unit>.NotFound();
        }

        // Session validation checks this flag, so deactivation signs out all members at once.
        organization.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        return Result<Unit>.Success(Unit.Value);
    }

    public async Task<Result<UserDto>> AddAdminAsync(int id, AddOrganizationAdminRequest request, CancellationToken ct)
    {
        var validation = await addAdminValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<UserDto>.ValidationFailed(validation.ToDictionary());
        }

        if (!await db.Organizations.AnyAsync(o => o.Id == id, ct))
        {
            return Result<UserDto>.NotFound();
        }

        var created = await users.CreateAsync(id, request.Email.Trim(), request.Password, DomainRole.Admin, ct);

        return created.IsSuccess
            ? Result<UserDto>.Success(created.Value.ToDto())
            : created.ToFailure<UserDto>();
    }

    private static OrganizationDto ToDto(Organization organization, int userCount) => new(
        organization.Id,
        organization.Name,
        organization.IsActive,
        userCount,
        organization.CreatedAtUtc.AsUtc());
}
