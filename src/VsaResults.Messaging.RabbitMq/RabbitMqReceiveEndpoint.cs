using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VsaResults;
using VsaResults.Messaging;

namespace VsaResults.Messaging.RabbitMq;

/// <summary>
/// RabbitMQ receive endpoint implementation.
/// </summary>
public class RabbitMqReceiveEndpoint : IReceiveEndpoint
{
    private readonly RabbitMqTransport _transport;
    private readonly RabbitMqTransportOptions _options;
    private readonly IMessageSerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqReceiveEndpointConfigurator _configurator;
    private readonly ILogger? _logger;

    private IChannel? _channel;
    private string? _consumerTag;
    private bool _isRunning;

    internal RabbitMqReceiveEndpoint(
        EndpointAddress address,
        RabbitMqTransport transport,
        RabbitMqTransportOptions options,
        IMessageSerializer serializer,
        IServiceProvider serviceProvider,
        RabbitMqReceiveEndpointConfigurator configurator,
        ILogger? logger)
    {
        Address = address;
        _transport = transport;
        _options = options;
        _serializer = serializer;
        _serviceProvider = serviceProvider;
        _configurator = configurator;
        _logger = logger;

        Name = address.Name;
    }

    /// <inheritdoc />
    public EndpointAddress Address { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public async Task<VsaResult<Unit>> StartAsync(CancellationToken ct = default)
    {
        if (_isRunning)
        {
            return Unit.Value;
        }

        try
        {
            // Ensure transport is connected
            var connectResult = await _transport.EnsureConnectedAsync(ct);
            if (connectResult.IsError)
            {
                return connectResult.Errors;
            }

            // Create a channel for this endpoint
            var channelResult = await _transport.CreateChannelAsync(ct);
            if (channelResult.IsError)
            {
                return channelResult.Errors;
            }

            _channel = channelResult.Value;

            // Declare the queue
            await _channel.QueueDeclareAsync(
                queue: Name,
                durable: _options.Durable,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            // Bind to exchanges for each message type
            foreach (var messageTypeName in _configurator.GetMessageTypes())
            {
                // Use same naming convention as RabbitMqPublishTransport
                var exchangeName = messageTypeName.Replace(':', '_').Replace('/', '_');

                // Declare the exchange (fanout for pub/sub)
                await _channel.ExchangeDeclareAsync(
                    exchange: exchangeName,
                    type: ExchangeType.Fanout,
                    durable: _options.Durable,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: ct);

                // Bind queue to exchange
                await _channel.QueueBindAsync(
                    queue: Name,
                    exchange: exchangeName,
                    routingKey: string.Empty,
                    arguments: null,
                    cancellationToken: ct);

                _logger?.LogDebug(
                    "Bound queue {Queue} to exchange {Exchange}",
                    Name,
                    exchangeName);
            }

            // Set prefetch count
            var prefetch = _configurator.PrefetchCount ?? _options.PrefetchCount;
            await _channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: (ushort)prefetch,
                global: false,
                cancellationToken: ct);

            // Create consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceivedAsync;

            // Start consuming
            _consumerTag = await _channel.BasicConsumeAsync(
                queue: Name,
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct);

            _isRunning = true;

            _logger?.LogInformation(
                "RabbitMQ endpoint '{EndpointName}' started with {ConsumerCount} consumer registrations",
                Name,
                _configurator.Consumers.Count);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start RabbitMQ endpoint '{EndpointName}'", Name);
            return MessagingErrors.TransportError($"Failed to start endpoint: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<VsaResult<Unit>> StopAsync(CancellationToken ct = default)
    {
        if (!_isRunning)
        {
            return Unit.Value;
        }

        try
        {
            if (_channel is not null && _consumerTag is not null)
            {
                await _channel.BasicCancelAsync(_consumerTag, cancellationToken: ct);
            }

            _isRunning = false;
            _logger?.LogInformation("RabbitMQ endpoint '{EndpointName}' stopped", Name);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping RabbitMQ endpoint '{EndpointName}'", Name);
            return MessagingErrors.TransportError($"Failed to stop endpoint: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();

        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
            _channel = null;
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            // Parse the message envelope from RabbitMQ properties
            var envelope = ParseEnvelope(ea);

            _logger?.LogDebug(
                "Received message {MessageId} on endpoint {Endpoint}",
                envelope.MessageId,
                Name);

            // Process the message
            await ProcessEnvelopeAsync(envelope, CancellationToken.None);

            // Acknowledge the message
            if (_channel is not null)
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing message on endpoint {Endpoint}", Name);

            // Negative acknowledge - requeue for retry
            if (_channel is not null)
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }
    }

    private MessageEnvelope ParseEnvelope(BasicDeliverEventArgs ea)
    {
        var props = ea.BasicProperties;
        var headers = props.Headers ?? new Dictionary<string, object?>();

        // Parse message types from the Type property
        var messageTypes = (props.Type ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        // Parse message ID
        var messageId = MessageId.New();
        if (!string.IsNullOrEmpty(props.MessageId))
        {
            var parseResult = MessageId.Parse(props.MessageId);
            if (!parseResult.IsError)
            {
                messageId = parseResult.Value;
            }
        }

        // Parse correlation ID
        var correlationId = CorrelationId.New();
        if (!string.IsNullOrEmpty(props.CorrelationId))
        {
            var parseResult = CorrelationId.Parse(props.CorrelationId);
            if (!parseResult.IsError)
            {
                correlationId = parseResult.Value;
            }
        }

        // Parse initiator ID from headers
        MessageId? initiatorId = null;
        if (headers.TryGetValue("vsa-initiator-id", out var initiatorValue) && initiatorValue is not null)
        {
            var initiatorStr = GetHeaderString(initiatorValue);
            if (!string.IsNullOrEmpty(initiatorStr))
            {
                var parseResult = MessageId.Parse(initiatorStr);
                if (!parseResult.IsError)
                {
                    initiatorId = parseResult.Value;
                }
            }
        }

        // Build custom headers from x- prefixed headers
        var customHeaders = new MessageHeaders();
        foreach (var (key, value) in headers)
        {
            if (key.StartsWith("x-") && value is not null)
            {
                var headerKey = key[2..]; // Remove "x-" prefix
                customHeaders[headerKey] = GetHeaderString(value);
            }
        }

        return new MessageEnvelope
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            InitiatorId = initiatorId,
            MessageTypes = messageTypes,
            Body = ea.Body.ToArray(),
            ContentType = props.ContentType ?? "application/json",
            Headers = customHeaders,
            SentTime = props.Timestamp.UnixTime > 0
                ? DateTimeOffset.FromUnixTimeSeconds(props.Timestamp.UnixTime)
                : DateTimeOffset.UtcNow
        };
    }

    private static string GetHeaderString(object value)
    {
        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string str => str,
            _ => value.ToString() ?? string.Empty
        };
    }

    private async Task ProcessEnvelopeAsync(MessageEnvelope envelope, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();

        foreach (var registration in _configurator.GetConsumerRegistrations())
        {
            // Check if this consumer handles this message type
            var primaryType = envelope.MessageTypes.FirstOrDefault();
            if (primaryType is null)
            {
                continue;
            }

            if (!registration.HandlesMessageType(primaryType))
            {
                continue;
            }

            await registration.InvokeAsync(scope.ServiceProvider, envelope, ct);
        }
    }
}

/// <summary>
/// RabbitMQ receive endpoint configurator.
/// </summary>
internal sealed class RabbitMqReceiveEndpointConfigurator : IReceiveEndpointConfigurator
{
    private readonly EndpointAddress _address;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<ConsumerRegistration> _consumers = new();
    private readonly List<string> _messageTypes = new();
    private readonly MessageTypeResolver _typeResolver = new();

    public RabbitMqReceiveEndpointConfigurator(EndpointAddress address, IServiceProvider serviceProvider)
    {
        _address = address;
        _serviceProvider = serviceProvider;
    }

    public string EndpointName => _address.Name;
    public IReadOnlyList<ConsumerRegistration> Consumers => _consumers;

    /// <summary>Gets or sets the prefetch count.</summary>
    public int? PrefetchCount { get; private set; }

    /// <summary>Gets or sets the concurrency limit.</summary>
    public int? ConcurrencyLimit { get; private set; }

    /// <inheritdoc />
    public void Consumer<TConsumer>()
        where TConsumer : class, IConsumer
    {
        var registration = new ConsumerRegistration(typeof(TConsumer), _serviceProvider);
        _consumers.Add(registration);

        // Register message types this consumer handles
        foreach (var messageType in registration.GetMessageTypeNames())
        {
            _messageTypes.Add(messageType);
        }
    }

    /// <inheritdoc />
    public void Consumer<TConsumer>(Func<IServiceProvider, TConsumer> factory)
        where TConsumer : class, IConsumer
    {
        var registration = new ConsumerRegistration(typeof(TConsumer), _serviceProvider, factory);
        _consumers.Add(registration);

        foreach (var messageType in registration.GetMessageTypeNames())
        {
            _messageTypes.Add(messageType);
        }
    }

    /// <inheritdoc />
    public void Handler<TMessage>(Func<ConsumeContext<TMessage>, CancellationToken, Task<VsaResult<Unit>>> handler)
        where TMessage : class, IMessage
    {
        var registration = new HandlerRegistration<TMessage>(handler, _serviceProvider);
        _consumers.Add(registration);

        foreach (var typeName in _typeResolver.GetMessageTypes<TMessage>())
        {
            _messageTypes.Add(typeName);
        }
    }

    /// <inheritdoc />
    public void UseRetry(IRetryPolicy policy)
    {
        // Stored for pipeline configuration
    }

    /// <inheritdoc />
    public void UseConcurrencyLimit(int limit)
    {
        ConcurrencyLimit = limit;
    }

    /// <inheritdoc />
    public void UsePrefetch(int count)
    {
        PrefetchCount = count;
    }

    /// <inheritdoc />
    public void UseCircuitBreaker(int failureThreshold, TimeSpan resetInterval)
    {
        // Stored for pipeline configuration
    }

    /// <inheritdoc />
    public void UseTimeout(TimeSpan timeout)
    {
        // Stored for pipeline configuration
    }

    /// <inheritdoc />
    public void UseFilter<TFilter>()
        where TFilter : class
    {
        // Stored for pipeline configuration
    }

    /// <inheritdoc />
    public void UseFilter<TContext>(IFilter<TContext> filter)
        where TContext : PipeContext
    {
        // Stored for pipeline configuration
    }

    /// <summary>Gets the registered message types.</summary>
    public IEnumerable<string> GetMessageTypes() => _messageTypes;

    /// <summary>Gets the consumer registrations.</summary>
    public IEnumerable<ConsumerRegistration> GetConsumerRegistrations() => _consumers;
}
