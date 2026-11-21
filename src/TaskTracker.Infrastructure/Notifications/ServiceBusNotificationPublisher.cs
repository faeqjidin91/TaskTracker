using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Abstractions;
using TaskTracker.Domain.Events;

namespace TaskTracker.Infrastructure.Notifications;

// Authenticates with DefaultAzureCredential (Managed Identity in Azure), so no connection
// string or key is needed. Registered as a singleton because ServiceBusClient is meant to
// be reused for the lifetime of the application.
public sealed class ServiceBusNotificationPublisher : INotificationPublisher, IAsyncDisposable
{
    private const string MessageSubject = "TaskStatusChanged";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusNotificationPublisher(IOptions<NotificationOptions> options)
    {
        var settings = options.Value.ServiceBus;
        _client = new ServiceBusClient(settings.FullyQualifiedNamespace, new DefaultAzureCredential());
        _sender = _client.CreateSender(settings.TopicName);
    }

    public async Task PublishAsync(TaskStatusChangedEvent evt, CancellationToken ct)
    {
        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(evt, JsonOptions))
        {
            MessageId = Guid.NewGuid().ToString(),
            Subject = MessageSubject,
            ContentType = "application/json"
        };
        message.ApplicationProperties["ProjectId"] = evt.ProjectId;
        message.ApplicationProperties["OrganizationId"] = evt.OrganizationId;

        await _sender.SendMessageAsync(message, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
