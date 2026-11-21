using TaskTracker.Application.Common;
using TaskTracker.Contracts.Tasks;
using TaskTracker.Domain.Entities;
using ContractPriority = TaskTracker.Contracts.Tasks.TaskPriority;
using ContractStatus = TaskTracker.Contracts.Tasks.TaskItemStatus;
using DomainPriority = TaskTracker.Domain.Enums.TaskPriority;
using DomainStatus = TaskTracker.Domain.Enums.TaskItemStatus;

namespace TaskTracker.Application.Mapping;

public static class TaskMappings
{
    public static TaskDto ToDto(this TaskItem task) => new(
        task.Id,
        task.ProjectId,
        task.Title,
        task.Description,
        task.Status.ToContract(),
        task.Priority.ToContract(),
        task.DueDateUtc.AsUtc(),
        task.CreatedAtUtc.AsUtc(),
        task.UpdatedAtUtc.AsUtc());

    // Explicit switches, not casts, so the two enums can never drift apart silently.
    public static ContractStatus ToContract(this DomainStatus status) => status switch
    {
        DomainStatus.Todo => ContractStatus.Todo,
        DomainStatus.InProgress => ContractStatus.InProgress,
        DomainStatus.Done => ContractStatus.Done,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown status.")
    };

    public static DomainStatus ToDomain(this ContractStatus status) => status switch
    {
        ContractStatus.Todo => DomainStatus.Todo,
        ContractStatus.InProgress => DomainStatus.InProgress,
        ContractStatus.Done => DomainStatus.Done,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown status.")
    };

    public static ContractPriority ToContract(this DomainPriority priority) => priority switch
    {
        DomainPriority.Low => ContractPriority.Low,
        DomainPriority.Medium => ContractPriority.Medium,
        DomainPriority.High => ContractPriority.High,
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown priority.")
    };

    public static DomainPriority ToDomain(this ContractPriority priority) => priority switch
    {
        ContractPriority.Low => DomainPriority.Low,
        ContractPriority.Medium => DomainPriority.Medium,
        ContractPriority.High => DomainPriority.High,
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown priority.")
    };
}
