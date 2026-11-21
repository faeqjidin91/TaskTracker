using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Events;

public record TaskStatusChangedEvent(
    int TaskId,
    int ProjectId,
    int OrganizationId,
    string Title,
    TaskItemStatus OldStatus,
    TaskItemStatus NewStatus,
    string ChangedBy,
    DateTime OccurredAtUtc);
