namespace TaskTracker.Contracts.Users;

// Duplicates the Domain enum for the same reason as TaskItemStatus: Web cannot see Domain.
public enum UserRole
{
    Viewer = 0,
    Member = 1,
    Admin = 2
}
