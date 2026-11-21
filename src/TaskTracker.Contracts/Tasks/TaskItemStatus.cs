namespace TaskTracker.Contracts.Tasks;

// Deliberately duplicates the Domain enum. Web references Contracts only, so sharing the
// Domain type would expose the domain model to the client. Duplicating keeps the API
// contract independent and gives compile-time safety that strings would not. The
// Application layer maps between the two explicitly.
public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}
