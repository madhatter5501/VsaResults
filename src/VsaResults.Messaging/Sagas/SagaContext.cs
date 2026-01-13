namespace VsaResults.Messaging;

/// <summary>
/// Context for saga execution, providing access to the saga state and messaging capabilities.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
public sealed class SagaContext<TState>
    where TState : class, ISagaState, new()
{
    private readonly IBus _bus;
    private readonly MessageEnvelope _envelope;
    private readonly List<object> _pendingPublishes = new();
    private readonly List<(EndpointAddress Address, object Message)> _pendingSends = new();

    internal SagaContext(
        TState state,
        IBus bus,
        MessageEnvelope envelope,
        bool isNew)
    {
        State = state;
        IsNew = isNew;
        _bus = bus;
        _envelope = envelope;
    }

    /// <summary>
    /// Gets the saga state instance.
    /// </summary>
    public TState State { get; }

    /// <summary>
    /// Gets a value indicating whether this is a new saga instance.
    /// </summary>
    public bool IsNew { get; }

    /// <summary>
    /// Gets the correlation ID for this saga instance.
    /// </summary>
    public Guid CorrelationId => State.CorrelationId;

    /// <summary>
    /// Gets the message envelope that triggered this saga execution.
    /// </summary>
    public MessageEnvelope Envelope => _envelope;

    /// <summary>
    /// Gets the message headers from the triggering message.
    /// </summary>
    public MessageHeaders Headers => _envelope.Headers;

    /// <summary>
    /// Transitions the saga to a new state.
    /// </summary>
    /// <param name="stateName">The name of the new state.</param>
    public void TransitionTo(string stateName)
    {
        State.CurrentState = stateName;
        State.ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the saga as complete, transitioning to the Final state.
    /// </summary>
    public void SetComplete()
    {
        TransitionTo("Final");
    }

    /// <summary>
    /// Marks the saga as faulted.
    /// </summary>
    public void SetFaulted()
    {
        TransitionTo("Faulted");
    }

    /// <summary>
    /// Queues an event to be published when the saga completes successfully.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="event">The event to publish.</param>
    public void Publish<TEvent>(TEvent @event)
        where TEvent : class, IEvent
    {
        _pendingPublishes.Add(@event);
    }

    /// <summary>
    /// Queues a command to be sent when the saga completes successfully.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="address">The destination endpoint address.</param>
    /// <param name="command">The command to send.</param>
    public void Send<TCommand>(EndpointAddress address, TCommand command)
        where TCommand : class, ICommand
    {
        _pendingSends.Add((address, command));
    }

    /// <summary>
    /// Executes all pending publishes and sends.
    /// Called internally after successful saga execution.
    /// </summary>
    internal async Task<ErrorOr<Unit>> FlushAsync(CancellationToken ct)
    {
        // Publish all pending events
        foreach (var @event in _pendingPublishes)
        {
            var publishMethod = typeof(IBus).GetMethod(nameof(IBus.PublishAsync), new[] { @event.GetType(), typeof(CancellationToken) });
            if (publishMethod is null)
            {
                continue;
            }

            var genericMethod = publishMethod.MakeGenericMethod(@event.GetType());
            var task = (Task<ErrorOr<Unit>>)genericMethod.Invoke(_bus, new object[] { @event, ct })!;
            var result = await task;

            if (result.IsError)
            {
                return result.Errors;
            }
        }

        // Send all pending commands
        foreach (var (address, command) in _pendingSends)
        {
            var endpointResult = await _bus.GetSendEndpointAsync(address, ct);
            if (endpointResult.IsError)
            {
                return endpointResult.Errors;
            }

            var sendMethod = typeof(ISendEndpoint).GetMethod(nameof(ISendEndpoint.SendAsync), new[] { command.GetType(), typeof(CancellationToken) });
            if (sendMethod is null)
            {
                continue;
            }

            var genericMethod = sendMethod.MakeGenericMethod(command.GetType());
            var task = (Task<ErrorOr<Unit>>)genericMethod.Invoke(endpointResult.Value, new object[] { command, ct })!;
            var result = await task;

            if (result.IsError)
            {
                return result.Errors;
            }
        }

        return Unit.Value;
    }
}
