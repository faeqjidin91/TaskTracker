namespace TaskTracker.Domain.Enums;

// Named TaskItemStatus because System.Threading.Tasks.TaskStatus is imported by implicit usings.
public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}
