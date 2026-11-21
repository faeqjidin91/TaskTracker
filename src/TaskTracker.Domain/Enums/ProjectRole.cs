namespace TaskTracker.Domain.Enums;

// The owner is not a role here: it is Project.OwnerUserId, so a project always has exactly one.
public enum ProjectRole
{
    Viewer = 0,
    Contributor = 1
}
