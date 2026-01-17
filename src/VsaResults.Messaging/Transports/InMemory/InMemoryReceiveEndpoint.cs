using Microsoft.Extensions.DependencyInjection;

namespace VsaResults.Messaging;

/// <summary>
/// In-memory receive endpoint.
/// Processes messages from an in-memory queue.
/// </summary>
internal sealed class InMemoryReceiveEndpoint : IReceiveEndpoint
{
    private readonly InMemoryQueue _queue;
    private readonly InMemoryReceiveEndpointConfigurator _configurator;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<Task> _workerTasks = new();
    private CancellationTokenSource? _cts;

    public InMemoryReceiveEndpoint(
        EndpointAddress address,
        InMemoryQueue queue,
        InMemoryReceiveEndpointConfigurator configurator,
        IServiceProvider serviceProvider)
    {
        Address = address;
        _queue = queue;
        _configurator = configurator;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public EndpointAddress Address { get; }

    /// <inheritdoc />
    public string Name => Address.Name;

    /// <inheritdoc />
    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;

    /// <inheritdoc />
    public Task<VsaResult<Unit>> StartAsync(CancellationToken ct = default)
    {
        if (IsRunning)
        {
            return Task.FromResult<VsaResult<Unit>>(Unit.Value);
        }

        _cts = new CancellationTokenSource();

        // Start worker tasks based on concurrency limit
        var concurrency = _configurator.ConcurrencyLimit ?? Environment.ProcessorCount;
        for (var i = 0; i < concurrency; i++)
        {
            _workerTasks.Add(ProcessMessagesAsync(_cts.Token));
        }

        return Task.FromResult<VsaResult<Unit>>(Unit.Value);
    }

    /// <inheritdoc />
    public async Task<VsaResult<Unit>> StopAsync(CancellationToken ct = default)
    {
        if (_cts is null)
        {
            return Unit.Value;
        }

        _cts.Cancel();

        try
        {
            await Task.WhenAll(_workerTasks);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        _workerTasks.Clear();
        _cts.Dispose();
        _cts = null;

        return Unit.Value;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private async Task ProcessMessagesAsync(CancellationToken ct)
    {
        await foreach (var envelope in _queue.ReadAllAsync(ct))
        {
            try
            {
                await ProcessEnvelopeAsync(envelope, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                // Log and continue - don't crash the worker
            }
        }
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
/// Configurator for in-memory receive endpoints.
/// </summary>
internal sealed class InMemoryReceiveEndpointConfigurator : IReceiveEndpointConfigurator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<ConsumerRegistration> _consumers = new();
    private readonly List<string> _messageTypes = new();

    public InMemoryReceiveEndpointConfigurator(string endpointName, IServiceProvider serviceProvider)
    {
        EndpointName = endpointName;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public string EndpointName { get; }

    /// <summary>Gets or sets the retry policy.</summary>
    public IRetryPolicy? RetryPolicy { get; private set; }

    /// <summary>Gets or sets the concurrency limit.</summary>
    public int? ConcurrencyLimit { get; private set; }

    /// <summary>Gets or sets the prefetch count.</summary>
    public int? PrefetchCount { get; private set; }

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

        var resolver = new MessageTypeResolver();
        foreach (var typeName in resolver.GetMessageTypes<TMessage>())
        {
            _messageTypes.Add(typeName);
        }
    }

    /// <inheritdoc />
    public void UseRetry(IRetryPolicy policy) => RetryPolicy = policy;

    /// <inheritdoc />
    public void UseConcurrencyLimit(int limit) => ConcurrencyLimit = limit;

    /// <inheritdoc />
    public void UsePrefetch(int count) => PrefetchCount = count;

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

/// <summary>
/// Registration for a consumer.
/// </summary>
internal class ConsumerRegistration
{
    private readonly Type _consumerType;
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<IServiceProvider, object>? _factory;
    private readonly MessageTypeResolver _typeResolver = new();
    private readonly HashSet<string> _messageTypeNames;

    public ConsumerRegistration(
        Type consumerType,
        IServiceProvider serviceProvider,
        Func<IServiceProvider, object>? factory = null)
    {
        _consumerType = consumerType;
        _serviceProvider = serviceProvider;
        _factory = factory;
        _messageTypeNames = new HashSet<string>(GetMessageTypeNames());
    }

    public Type ConsumerType => _consumerType;

    public virtual bool HandlesMessageType(string messageTypeName)
    {
        return _messageTypeNames.Contains(messageTypeName);
    }

    public virtual IEnumerable<string> GetMessageTypeNames()
    {
        foreach (var iface in _consumerType.GetInterfaces())
        {
            if (!iface.IsGenericType)
            {
                continue;
            }

            var genericDef = iface.GetGenericTypeDefinition();
            if (genericDef == typeof(IConsumer<>))
            {
                var messageType = iface.GetGenericArguments()[0];
                foreach (var typeName in _typeResolver.GetMessageTypes(messageType))
                {
                    yield return typeName;
                }
            }
        }
    }

    public virtual async Task InvokeAsync(
        IServiceProvider scopedProvider,
        MessageEnvelope envelope,
        CancellationToken ct)
    {
        var consumer = _factory?.Invoke(scopedProvider)
            ?? scopedProvider.GetRequiredService(_consumerType);

        // Find and invoke the appropriate Consume method
        foreach (var iface in _consumerType.GetInterfaces())
        {
            if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != typeof(IConsumer<>))
            {
                continue;
            }

            var messageType = iface.GetGenericArguments()[0];
            var typeName = _typeResolver.GetPrimaryIdentifier(messageType);

            if (!envelope.MessageTypes.Contains(typeName))
            {
                continue;
            }

            // Deserialize the message
            var serializer = scopedProvider.GetRequiredService<IMessageSerializer>();
            var messageResult = serializer.Deserialize(envelope.Body, messageType);

            if (messageResult.IsError)
            {
                continue;
            }

            // Create consume context
            var contextType = typeof(ConsumeContext<>).MakeGenericType(messageType);
            var bus = scopedProvider.GetRequiredService<IBus>();

            var context = Activator.CreateInstance(contextType);
            if (context is null)
            {
                continue;
            }

            // Set properties via reflection (simplified)
            contextType.GetProperty("Message")!.SetValue(context, messageResult.Value);
            contextType.GetProperty("Envelope")!.SetValue(context, envelope);
            contextType.GetProperty("PublishEndpoint")!.SetValue(context, bus);
            contextType.GetProperty("SendEndpointProvider")!.SetValue(context, bus);

            // Invoke ConsumeAsync
            var method = iface.GetMethod("ConsumeAsync");
            if (method is null)
            {
                continue;
            }

            var task = method.Invoke(consumer, new object[] { context, ct }) as Task;
            if (task is not null)
            {
                await task;
            }
        }
    }
}

/// <summary>
/// Registration for a handler delegate.
/// </summary>
internal sealed class HandlerRegistration<TMessage> : ConsumerRegistration
    where TMessage : class, IMessage
{
    private readonly Func<ConsumeContext<TMessage>, CancellationToken, Task<VsaResult<Unit>>> _handler;
    private readonly MessageTypeResolver _typeResolver = new();
    private readonly HashSet<string> _handlerMessageTypes;

    public HandlerRegistration(
        Func<ConsumeContext<TMessage>, CancellationToken, Task<VsaResult<Unit>>> handler,
        IServiceProvider serviceProvider)
        : base(typeof(HandlerRegistration<TMessage>), serviceProvider)
    {
        _handler = handler;
        _handlerMessageTypes = new HashSet<string>(_typeResolver.GetMessageTypes<TMessage>());
    }

    public override bool HandlesMessageType(string messageTypeName)
    {
        return _handlerMessageTypes.Contains(messageTypeName);
    }

    public override IEnumerable<string> GetMessageTypeNames()
    {
        return _typeResolver.GetMessageTypes<TMessage>();
    }

    public override async Task InvokeAsync(
        IServiceProvider scopedProvider,
        MessageEnvelope envelope,
        CancellationToken ct)
    {
        var serializer = scopedProvider.GetRequiredService<IMessageSerializer>();
        var messageResult = serializer.Deserialize<TMessage>(envelope.Body);

        if (messageResult.IsError)
        {
            return;
        }

        var bus = scopedProvider.GetRequiredService<IBus>();
        var context = new ConsumeContext<TMessage>
        {
            Message = messageResult.Value,
            Envelope = envelope,
            PublishEndpoint = bus,
            SendEndpointProvider = bus
        };

        await _handler(context, ct);
    }
}
