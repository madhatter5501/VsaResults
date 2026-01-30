using System.Collections.Concurrent;
using VsaResults.Messaging;
using VsaResults.WideEvents;

namespace Tests.Integration;

/// <summary>
/// Test implementation of IWideEventEmitter that captures events for verification.
/// </summary>
public sealed class TestWideEventEmitter : IWideEventEmitter
{
    private readonly ConcurrentBag<WideEvent> _events = new();
    private readonly List<Action<WideEvent>> _callbacks = new();
    private volatile WideEvent? _lastEvent;

    /// <summary>
    /// Gets all emitted events.
    /// </summary>
    public IReadOnlyCollection<WideEvent> Events => _events.ToArray();

    /// <summary>
    /// Gets the most recent event.
    /// </summary>
    public WideEvent? LastEvent => _lastEvent;

    /// <summary>
    /// Gets events by feature name.
    /// </summary>
    public IEnumerable<WideEvent> GetEventsByFeature(string featureName)
        => _events.Where(e => e.Feature?.FeatureName == featureName);

    /// <summary>
    /// Gets events by outcome.
    /// </summary>
    public IEnumerable<WideEvent> GetSuccessfulEvents()
        => _events.Where(e => e.Outcome == "success");

    /// <summary>
    /// Gets events with errors.
    /// </summary>
    public IEnumerable<WideEvent> GetFailedEvents()
        => _events.Where(e => e.Outcome != "success");

    /// <summary>
    /// Registers a callback to be invoked when an event is emitted.
    /// </summary>
    public void OnEmit(Action<WideEvent> callback)
    {
        _callbacks.Add(callback);
    }

    /// <inheritdoc />
    public ValueTask EmitAsync(WideEvent wideEvent, CancellationToken ct = default)
    {
        _events.Add(wideEvent);
        _lastEvent = wideEvent;
        foreach (var callback in _callbacks)
        {
            callback(wideEvent);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void Emit(WideEvent wideEvent)
    {
        _events.Add(wideEvent);
        _lastEvent = wideEvent;
        foreach (var callback in _callbacks)
        {
            callback(wideEvent);
        }
    }

    /// <summary>
    /// Clears all captured events.
    /// </summary>
    public void Clear()
    {
        while (_events.TryTake(out _)) { }
        _lastEvent = null;
        _callbacks.Clear();
    }

    /// <summary>
    /// Waits for an event matching the predicate.
    /// </summary>
    public async Task<WideEvent> WaitForEventAsync(
        Func<WideEvent, bool> predicate,
        TimeSpan? timeout = null)
    {
        var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
        var tcs = new TaskCompletionSource<WideEvent>();

        // Check existing events first
        var existing = _events.FirstOrDefault(predicate);
        if (existing is not null)
        {
            return existing;
        }

        // Register callback for future events
        void Callback(WideEvent e)
        {
            if (predicate(e))
            {
                tcs.TrySetResult(e);
            }
        }

        OnEmit(Callback);

        // Check again after registering (race condition protection)
        existing = _events.FirstOrDefault(predicate);
        if (existing is not null)
        {
            return existing;
        }

        cts.Token.Register(() => tcs.TrySetCanceled());

        return await tcs.Task;
    }
}

/// <summary>
/// Test implementation of IMessageWideEventEmitter that captures events for verification.
/// </summary>
public sealed class TestMessageWideEventEmitter : IMessageWideEventEmitter
{
    private readonly ConcurrentBag<MessageWideEvent> _events = new();
    private readonly List<Action<MessageWideEvent>> _callbacks = new();
    private volatile MessageWideEvent? _lastEvent;

    /// <summary>
    /// Gets all emitted events.
    /// </summary>
    public IReadOnlyCollection<MessageWideEvent> Events => _events.ToArray();

    /// <summary>
    /// Gets the most recent event.
    /// </summary>
    public MessageWideEvent? LastEvent => _lastEvent;

    /// <summary>
    /// Gets events by message type.
    /// </summary>
    public IEnumerable<MessageWideEvent> GetEventsByMessageType(string messageType)
        => _events.Where(e => e.MessageType == messageType);

    /// <summary>
    /// Gets events by consumer type.
    /// </summary>
    public IEnumerable<MessageWideEvent> GetEventsByConsumer(string consumerType)
        => _events.Where(e => e.ConsumerType == consumerType);

    /// <summary>
    /// Gets events by correlation ID.
    /// </summary>
    public IEnumerable<MessageWideEvent> GetEventsByCorrelationId(string correlationId)
        => _events.Where(e => e.CorrelationId == correlationId);

    /// <summary>
    /// Gets successful events.
    /// </summary>
    public IEnumerable<MessageWideEvent> GetSuccessfulEvents()
        => _events.Where(e => e.Outcome == "success");

    /// <summary>
    /// Gets failed events.
    /// </summary>
    public IEnumerable<MessageWideEvent> GetFailedEvents()
        => _events.Where(e => e.Outcome != "success");

    /// <summary>
    /// Registers a callback to be invoked when an event is emitted.
    /// </summary>
    public void OnEmit(Action<MessageWideEvent> callback)
    {
        _callbacks.Add(callback);
    }

    /// <inheritdoc />
    public void Emit(MessageWideEvent wideEvent)
    {
        _events.Add(wideEvent);
        _lastEvent = wideEvent;
        foreach (var callback in _callbacks)
        {
            callback(wideEvent);
        }
    }

    /// <summary>
    /// Clears all captured events.
    /// </summary>
    public void Clear()
    {
        while (_events.TryTake(out _)) { }
        _lastEvent = null;
        _callbacks.Clear();
    }

    /// <summary>
    /// Waits for an event matching the predicate.
    /// </summary>
    public async Task<MessageWideEvent> WaitForEventAsync(
        Func<MessageWideEvent, bool> predicate,
        TimeSpan? timeout = null)
    {
        var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
        var tcs = new TaskCompletionSource<MessageWideEvent>();

        // Check existing events first
        var existing = _events.FirstOrDefault(predicate);
        if (existing is not null)
        {
            return existing;
        }

        // Register callback for future events
        void Callback(MessageWideEvent e)
        {
            if (predicate(e))
            {
                tcs.TrySetResult(e);
            }
        }

        OnEmit(Callback);

        // Check again after registering (race condition protection)
        existing = _events.FirstOrDefault(predicate);
        if (existing is not null)
        {
            return existing;
        }

        cts.Token.Register(() => tcs.TrySetCanceled());

        return await tcs.Task;
    }
}
