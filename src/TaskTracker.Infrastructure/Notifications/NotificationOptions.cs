namespace TaskTracker.Infrastructure.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    // Starts empty because the configuration binder appends to existing items.
    // When no providers are configured, registration falls back to Logging.
    public List<string> Providers { get; set; } = [];
    public ServiceBusOptions ServiceBus { get; set; } = new();
}

public sealed class ServiceBusOptions
{
    public string FullyQualifiedNamespace { get; set; } = string.Empty;
    public string TopicName { get; set; } = "task-events";
}
