using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Api.Auth;
using TaskTracker.Api.Common;
using TaskTracker.Application.Services;
using TaskTracker.Contracts.Organizations;
using TaskTracker.Contracts.Users;

namespace TaskTracker.Api.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize(Policy = Policies.SuperAdmin)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class OrganizationsController(IOrganizationService organizations) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrganizationDto>>> GetAll(CancellationToken ct) =>
        this.ToOk(await organizations.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganizationDto>> GetById(int id, CancellationToken ct) =>
        this.ToOk(await organizations.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrganizationDto>> Create(CreateOrganizationRequest request, CancellationToken ct) =>
        this.ToCreated(
            await organizations.CreateAsync(request, ct),
            nameof(GetById),
            "Organizations",
            organization => new { id = organization.Id });

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrganizationStatusRequest request, CancellationToken ct) =>
        this.ToNoContent(await organizations.UpdateStatusAsync(id, request, ct));

    [HttpPost("{id:int}/admins")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> AddAdmin(int id, AddOrganizationAdminRequest request, CancellationToken ct) =>
        this.ToCreated(
            await organizations.AddAdminAsync(id, request, ct),
            nameof(GetById),
            "Organizations",
            _ => new { id });
}
