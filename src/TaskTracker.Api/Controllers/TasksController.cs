using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Api.Auth;
using TaskTracker.Api.Common;
using TaskTracker.Application.Services;
using TaskTracker.Contracts.Tasks;

namespace TaskTracker.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize(Policy = Policies.InOrganization)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public class TasksController(ITaskService tasks) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskDto>> GetById(int id, CancellationToken ct) =>
        this.ToOk(await tasks.GetByIdAsync(id, ct));

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, UpdateTaskRequest request, CancellationToken ct) =>
        this.ToNoContent(await tasks.UpdateAsync(id, request, ct));

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateTaskStatusRequest request, CancellationToken ct) =>
        this.ToNoContent(await tasks.UpdateStatusAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        this.ToNoContent(await tasks.DeleteAsync(id, ct));
}
