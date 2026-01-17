using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using VsaResults;
using VsaResults.Messaging;

namespace VsaResults.Messaging.AzureServiceBus;

/// <summary>
/// Azure Service Bus publish transport for pub/sub messaging via topics.
/// </summary>
public class AzureServiceBusPublishTransport : IPublishTransport
{
    private readonly AzureServiceBusTransport _transport;
    private readonly AzureServiceBusTransportOptions _options;
    private readonly ILogger? _logger;
    private readonly MessageTypeResolver _typeResolver = new();

    internal AzureServiceBusPublishTransport(
        AzureServiceBusTransport transport,
        AzureServiceBusTransportOptions options,
        ILogger? logger)
    {
        _transport = transport;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ErrorOr<Unit>> PublishAsync<TMessage>(
        MessageEnvelope envelope,
        CancellationToken ct = default)
        where TMessage : class, IEvent
    {
        try
        {
            // Use the message type URN as the topic name (normalized for Service Bus)
            var topicName = GetTopicName<TMessage>();

            // Get or create a sender for this topic
            var senderResult = await _transport.GetOrCreateSenderAsync(topicName, isQueue: false, ct);
            if (senderResult.IsError)
            {
                return senderResult.Errors;
            }

            var message = AzureServiceBusSendTransport.CreateServiceBusMessage(envelope);

            await senderResult.Value.SendMessageAsync(message, ct);

            _logger?.LogDebug(
                "Published {MessageType} to topic {Topic}, MessageId {MessageId}",
                typeof(TMessage).Name,
                topicName,
                message.MessageId);

            return Unit.Value;
        }
        catch (ServiceBusException ex)
        {
            _logger?.LogError(
                ex,
                "Failed to publish {MessageType}: {Reason}",
                typeof(TMessage).Name,
                ex.Reason);
            return MessagingErrors.TransportError($"Failed to publish message: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to publish {MessageType}", typeof(TMessage).Name);
            return MessagingErrors.TransportError($"Failed to publish message: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the Service Bus topic name for a message type.
    /// Uses the URN format from MessageTypeResolver for consistency with consumer subscriptions.
    /// </summary>
    protected virtual string GetTopicName<TMessage>()
        where TMessage : class, IEvent
    {
        // Use the URN format that MessageTypeResolver produces
        // urn:message:Namespace:TypeName becomes topic name normalized for Service Bus
        var primaryType = _typeResolver.GetPrimaryIdentifier<TMessage>();
        return NormalizeTopicName(primaryType);
    }

    /// <summary>
    /// Normalizes a topic name to be Azure Service Bus-compliant.
    /// </summary>
    internal static string NormalizeTopicName(string name)
    {
        // Azure Service Bus entity names:
        // - Can contain letters, numbers, periods, hyphens, underscores
        // - Must start with a letter or number
        // - Max 260 characters
        // - Case-insensitive
        return name
            .Replace(':', '-')
            .Replace('/', '-')
            .Replace(' ', '-')
            .ToLowerInvariant()
            .TrimStart('-');
    }
}
