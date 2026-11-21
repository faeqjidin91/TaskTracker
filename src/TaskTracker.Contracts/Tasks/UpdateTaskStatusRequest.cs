using System.Text.Json.Serialization;

namespace TaskTracker.Contracts.Tasks;

public record UpdateTaskStatusRequest([property: JsonRequired] TaskItemStatus Status);
