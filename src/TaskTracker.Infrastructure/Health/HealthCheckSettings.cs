namespace TaskTracker.Infrastructure.Health;

public static class HealthCheckSettings
{
    public const string ReadyTag = "ready";

    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
}
