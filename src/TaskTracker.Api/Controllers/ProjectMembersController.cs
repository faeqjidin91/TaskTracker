using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Api.Auth;
using TaskTracker.Api.Common;
using TaskTracker.Application.Services;
using TaskTracker.Contracts.Projects;

namespace TaskTracker.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}")]
[Authorize(Policy = Policies.InOrganization)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public class ProjectMembersController(IProjectMemberService members) : ControllerBase
{
    [HttpGet("members")]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectMemberDto>>> GetMembers(int projectId, CancellationToken ct) =>
        this.ToOk(await members.GetMembersAsync(projectId, ct));

    [HttpGet("member-candidates")]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectCandidateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectCandidateDto>>> GetCandidates(int projectId, CancellationToken ct) =>
        this.ToOk(await members.GetCandidatesAsync(projectId, ct));

    [HttpPost("members")]
    [ProducesResponseType(typeof(ProjectMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectMemberDto>> Add(int projectId, AddProjectMemberRequest request, CancellationToken ct) =>
        this.ToCreated(
            await members.AddAsync(projectId, request, ct),
            nameof(GetMembers),
            "ProjectMembers",
            _ => new { projectId });

    [HttpPatch("members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int projectId, string userId, UpdateProjectMemberRequest request, CancellationToken ct) =>
        this.ToNoContent(await members.UpdateAsync(projectId, userId, request, ct));

    [HttpDelete("members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(int projectId, string userId, CancellationToken ct) =>
        this.ToNoContent(await members.RemoveAsync(projectId, userId, ct));

    [HttpPatch("owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransferOwner(int projectId, TransferProjectOwnerRequest request, CancellationToken ct) =>
        this.ToNoContent(await members.TransferOwnerAsync(projectId, request, ct));
}
