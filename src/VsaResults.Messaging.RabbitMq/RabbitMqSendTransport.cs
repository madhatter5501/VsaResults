using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using VsaResults;
using VsaResults.Messaging;

namespace VsaResults.Messaging.RabbitMq;

/// <summary>
/// RabbitMQ send transport for point-to-point messaging.
/// </summary>
public class RabbitMqSendTransport : ISendTransport
{
    private readonly IChannel _channel;
    private readonly RabbitMqTransportOptions _options;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger? _logger;
    private readonly HashSet<string> _declaredQueues = new();
    private readonly SemaphoreSlim _declareLock = new(1, 1);

    internal RabbitMqSendTransport(
        EndpointAddress address,
        IChannel channel,
        RabbitMqTransportOptions options,
        IMessageSerializer serializer,
        ILogger? logger)
    {
        Address = address;
        _channel = channel;
        _options = options;
        _serializer = serializer;
        _logger = logger;
    }

    /// <inheritdoc />
    public EndpointAddress Address { get; }

    /// <inheritdoc />
    public async Task<VsaResult<Unit>> SendAsync<TMessage>(
        MessageEnvelope envelope,
        CancellationToken ct = default)
        where TMessage : class, IMessage
    {
        try
        {
            var queueName = Address.Name;

            // Ensure queue exists
            await EnsureQueueDeclaredAsync(queueName, ct);

            // Create message properties
            var properties = new BasicProperties
            {
                MessageId = envelope.MessageId.ToString(),
                CorrelationId = envelope.CorrelationId.ToString(),
                ContentType = envelope.ContentType ?? "application/json",
                DeliveryMode = _options.PersistentMessages ? DeliveryModes.Persistent : DeliveryModes.Transient,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Type = string.Join(";", envelope.MessageTypes),
                Headers = ConvertHeaders(envelope)
            };

            // Send directly to the queue using default exchange
            // With RabbitMQ's default exchange, routing key = queue name
            await _channel.BasicPublishAsync(
                exchange: string.Empty, // Default exchange
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: envelope.Body,
                cancellationToken: ct);

            _logger?.LogDebug(
                "Sent {MessageType} to queue {Queue}",
                typeof(TMessage).Name,
                queueName);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send {MessageType} to {Address}", typeof(TMessage).Name, Address);
            return MessagingErrors.DeliveryFailed(Address, ex.Message);
        }
    }

    /// <summary>
    /// Ensures the queue is declared.
    /// </summary>
    private async Task EnsureQueueDeclaredAsync(string queueName, CancellationToken ct)
    {
        if (_declaredQueues.Contains(queueName))
        {
            return;
        }

        await _declareLock.WaitAsync(ct);
        try
        {
            if (_declaredQueues.Contains(queueName))
            {
                return;
            }

            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: _options.Durable,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            _declaredQueues.Add(queueName);

            _logger?.LogDebug("Declared queue {Queue}", queueName);
        }
        finally
        {
            _declareLock.Release();
        }
    }

    /// <summary>
    /// Converts envelope headers to RabbitMQ-compatible dictionary.
    /// </summary>
    private static Dictionary<string, object?> ConvertHeaders(MessageEnvelope envelope)
    {
        var headers = new Dictionary<string, object?>
        {
            ["vsa-message-id"] = envelope.MessageId.ToString(),
            ["vsa-correlation-id"] = envelope.CorrelationId.ToString(),
            ["vsa-sent-time"] = envelope.SentTime.ToString("O"),
            ["vsa-source-address"] = envelope.SourceAddress?.ToString(),
            ["vsa-destination-address"] = envelope.DestinationAddress?.ToString()
        };

        if (envelope.InitiatorId is not null)
        {
            headers["vsa-initiator-id"] = envelope.InitiatorId.ToString();
        }

        if (envelope.ConversationId is not null)
        {
            headers["vsa-conversation-id"] = envelope.ConversationId.ToString();
        }

        // Add custom headers
        foreach (var (key, value) in envelope.Headers)
        {
            headers[$"x-{key}"] = value;
        }

        // Add host info
        if (envelope.Host is not null)
        {
            headers["vsa-host-machine"] = envelope.Host.MachineName;
            headers["vsa-host-process"] = envelope.Host.ProcessName;
            headers["vsa-host-process-id"] = envelope.Host.ProcessId.ToString();
        }

        return headers;
    }
}
