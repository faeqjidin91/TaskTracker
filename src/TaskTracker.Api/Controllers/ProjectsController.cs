using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Api.Auth;
using TaskTracker.Api.Common;
using TaskTracker.Application.Services;
using TaskTracker.Contracts.Projects;
using TaskTracker.Contracts.Tasks;

namespace TaskTracker.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(IProjectService projects, ITaskService tasks) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> GetAll(CancellationToken ct) =>
        this.ToOk(await projects.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetById(int id, CancellationToken ct) =>
        this.ToOk(await projects.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Policies.CanManageProjects)]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request, CancellationToken ct) =>
        this.ToCreated(
            await projects.CreateAsync(request, ct),
            nameof(GetById),
            "Projects",
            project => new { id = project.Id });

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.CanManageProjects)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateProjectRequest request, CancellationToken ct) =>
        this.ToNoContent(await projects.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.CanManageProjects)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        this.ToNoContent(await projects.DeleteAsync(id, ct));

    [HttpGet("{id:int}/tasks")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetTasks(int id, CancellationToken ct) =>
        this.ToOk(await tasks.GetByProjectAsync(id, ct));

    [HttpPost("{id:int}/tasks")]
    [Authorize(Policy = Policies.CanManageTasks)]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> CreateTask(int id, CreateTaskRequest request, CancellationToken ct) =>
        this.ToCreated(
            await tasks.CreateAsync(id, request, ct),
            nameof(TasksController.GetById),
            "Tasks",
            task => new { id = task.Id });
}
