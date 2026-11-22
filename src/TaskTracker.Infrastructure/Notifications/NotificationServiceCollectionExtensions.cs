using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TaskTracker.Application.Abstractions;
using TaskTracker.Infrastructure.Health;

namespace TaskTracker.Infrastructure.Notifications;

public static class NotificationServiceCollectionExtensions
{
    private const string DefaultProvider = "Logging";
    private const string ServiceBusProvider = "ServiceBus";

    private static readonly Dictionary<string, Type> KnownProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        [DefaultProvider] = typeof(LoggingNotificationPublisher),
        ["Database"] = typeof(DatabaseNotificationPublisher),
        [ServiceBusProvider] = typeof(ServiceBusNotificationPublisher)
    };

    public static IServiceCollection AddNotifications(
        this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(NotificationOptions.SectionName);
        var options = section.Get<NotificationOptions>() ?? new NotificationOptions();
        services.Configure<NotificationOptions>(section);

        var names = options.Providers.Count > 0 ? options.Providers : [DefaultProvider];
        var providerTypes = names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(ResolveProvider)
            .ToList();

        if (providerTypes.Contains(typeof(ServiceBusNotificationPublisher)))
        {
            ValidateServiceBus(options.ServiceBus);
            services.AddHealthChecks().AddCheck<ServiceBusHealthCheck>(
                "service-bus",
                HealthStatus.Degraded,
                [HealthCheckSettings.ReadyTag],
                HealthCheckSettings.Timeout);
        }

        foreach (var providerType in providerTypes)
        {
            services.AddSingleton(providerType);
        }

        services.AddSingleton<INotificationPublisher>(provider => new CompositeNotificationPublisher(
            providerTypes.Select(type => (INotificationPublisher)provider.GetRequiredService(type)).ToList(),
            provider.GetRequiredService<ILogger<CompositeNotificationPublisher>>()));

        return services;
    }

    private static Type ResolveProvider(string name) =>
        KnownProviders.TryGetValue(name, out var type)
            ? type
            : throw new InvalidOperationException(
                $"Unknown notification provider '{name}' in {NotificationOptions.SectionName}:Providers. " +
                $"Valid providers: {string.Join(", ", KnownProviders.Keys)}.");

    private static void ValidateServiceBus(ServiceBusOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace))
        {
            throw new InvalidOperationException(
                $"{NotificationOptions.SectionName}:{ServiceBusProvider}:FullyQualifiedNamespace is required " +
                "when the ServiceBus provider is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.TopicName))
        {
            throw new InvalidOperationException(
                $"{NotificationOptions.SectionName}:{ServiceBusProvider}:TopicName is required " +
                "when the ServiceBus provider is enabled.");
        }
    }
}
