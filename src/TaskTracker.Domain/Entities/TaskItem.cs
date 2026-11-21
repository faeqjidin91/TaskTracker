using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities;

public class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public required string Title { get; set; }
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Sets the new status and update time.</summary>
    /// <returns>The status before the change.</returns>
    public TaskItemStatus ChangeStatus(TaskItemStatus newStatus, DateTime nowUtc)
    {
        var oldStatus = Status;
        Status = newStatus;
        UpdatedAtUtc = nowUtc;
        return oldStatus;
    }
}
