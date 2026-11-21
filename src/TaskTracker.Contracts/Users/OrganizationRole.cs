namespace TaskTracker.Contracts.Users;

// Duplicates the Domain enum for the same reason as TaskItemStatus: Web cannot see Domain.
public enum OrganizationRole
{
    Member = 0,
    Admin = 1
}
