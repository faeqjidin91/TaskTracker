using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskTracker.Infrastructure.Notifications;

namespace TaskTracker.Infrastructure.Health;

// A failure is reported as Degraded by its registration, because notifications never block requests.
public sealed class ServiceBusHealthCheck(ServiceBusNotificationPublisher publisher) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await publisher.CheckConnectionAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
