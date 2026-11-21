using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Common;
using TaskTracker.Application.Mapping;
using TaskTracker.Contracts.Tasks;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Events;

namespace TaskTracker.Application.Services;

public class TaskService(
    IAppDbContext db,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskRequest> updateValidator,
    IValidator<UpdateTaskStatusRequest> statusValidator,
    INotificationPublisher publisher,
    ICurrentUser currentUser,
    IProjectAccess access,
    TimeProvider timeProvider,
    ILogger<TaskService> logger) : ITaskService
{
    private const string UnknownUser = "unknown";

    public async Task<Result<IReadOnlyList<TaskDto>>> GetByProjectAsync(int projectId, CancellationToken ct)
    {
        if (await access.GetAsync(projectId, ct) is null)
        {
            return Result<IReadOnlyList<TaskDto>>.NotFound();
        }

        var tasks = await db.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

        IReadOnlyList<TaskDto> dtos = tasks.Select(t => t.ToDto()).ToList();
        return Result<IReadOnlyList<TaskDto>>.Success(dtos);
    }

    public async Task<Result<TaskDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

        if (task is null || await access.GetAsync(task.ProjectId, ct) is null)
        {
            return Result<TaskDto>.NotFound();
        }

        return Result<TaskDto>.Success(task.ToDto());
    }

    public async Task<Result<TaskDto>> CreateAsync(int projectId, CreateTaskRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<TaskDto>.ValidationFailed(validation.ToDictionary());
        }

        var denied = await DenyTaskChangeAsync<TaskDto>(projectId, ct);
        if (denied is not null)
        {
            return denied;
        }

        var now = UtcNow();
        var task = new TaskItem
        {
            ProjectId = projectId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Priority = request.Priority.ToDomain(),
            DueDateUtc = request.DueDateUtc.AsUtc(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);

        return Result<TaskDto>.Success(task.ToDto());
    }

    public async Task<Result<Unit>> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailed(validation.ToDictionary());
        }

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
        {
            return Result<Unit>.NotFound();
        }

        var denied = await DenyTaskChangeAsync<Unit>(task.ProjectId, ct);
        if (denied is not null)
        {
            return denied;
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Priority = request.Priority.ToDomain();
        task.DueDateUtc = request.DueDateUtc.AsUtc();
        task.UpdatedAtUtc = UtcNow();
        await db.SaveChangesAsync(ct);

        return Result<Unit>.Success(Unit.Value);
    }

    public async Task<Result<Unit>> UpdateStatusAsync(int id, UpdateTaskStatusRequest request, CancellationToken ct)
    {
        var validation = await statusValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailed(validation.ToDictionary());
        }

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
        {
            return Result<Unit>.NotFound();
        }

        var denied = await DenyTaskChangeAsync<Unit>(task.ProjectId, ct);
        if (denied is not null)
        {
            return denied;
        }

        var newStatus = request.Status.ToDomain();
        if (task.Status == newStatus)
        {
            return Result<Unit>.Success(Unit.Value);
        }

        var now = UtcNow();
        var oldStatus = task.ChangeStatus(newStatus, now);
        await db.SaveChangesAsync(ct);

        await PublishStatusChangedAsync(new TaskStatusChangedEvent(
            task.Id,
            task.ProjectId,
            currentUser.OrganizationId ?? 0,
            task.Title,
            oldStatus,
            newStatus,
            currentUser.Email ?? UnknownUser,
            now));

        return Result<Unit>.Success(Unit.Value);
    }

    public async Task<Result<Unit>> DeleteAsync(int id, CancellationToken ct)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
        {
            return Result<Unit>.NotFound();
        }

        var denied = await DenyTaskChangeAsync<Unit>(task.ProjectId, ct);
        if (denied is not null)
        {
            return denied;
        }

        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);

        return Result<Unit>.Success(Unit.Value);
    }

    // Hidden projects return 404, so a user cannot probe which ids exist.
    private async Task<Result<T>?> DenyTaskChangeAsync<T>(int projectId, CancellationToken ct)
    {
        var permissions = await access.GetAsync(projectId, ct);

        if (permissions is null)
        {
            return Result<T>.NotFound();
        }

        return permissions.CanManageTasks ? null : Result<T>.Forbidden();
    }

    // Runs after the save, so a failure here must not fail the request. The change is already
    // committed, which is also why the caller's token is not used: a client disconnect should
    // not cancel the notification. A crash between save and publish still loses the event.
    // Production upgrade: the outbox pattern, writing the event in the same transaction as the
    // status change and publishing it from a background worker.
    private async Task PublishStatusChangedAsync(TaskStatusChangedEvent evt)
    {
        try
        {
            await publisher.PublishAsync(evt, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish status change for task {TaskId}", evt.TaskId);
        }
    }

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;
}
