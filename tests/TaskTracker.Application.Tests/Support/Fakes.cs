using Microsoft.Extensions.Logging;
using TaskTracker.Application.Abstractions;
using TaskTracker.Domain.Events;

namespace TaskTracker.Application.Tests.Support;

public sealed class FakeCurrentUser : ICurrentUser
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public int? OrganizationId { get; set; }
    public bool IsOrganizationAdmin { get; set; }
    public bool IsSuperAdmin { get; set; }
}

public sealed class FakeNotificationPublisher : INotificationPublisher
{
    public List<TaskStatusChangedEvent> Events { get; } = [];
    public bool ThrowOnPublish { get; set; }
    public Action<TaskStatusChangedEvent>? OnPublish { get; set; }
    public CancellationToken LastToken { get; private set; }

    public Task PublishAsync(TaskStatusChangedEvent evt, CancellationToken ct)
    {
        LastToken = ct;
        Events.Add(evt);
        OnPublish?.Invoke(evt);

        return ThrowOnPublish
            ? throw new InvalidOperationException("Simulated publisher failure.")
            : Task.CompletedTask;
    }
}

public sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(utcNow);
}

public sealed class RecordingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception)));
}
