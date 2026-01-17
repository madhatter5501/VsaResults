namespace VsaResults.WideEvents;

/// <summary>
/// Abstraction for writing wide events to a destination.
/// Sinks are the final stage of the emission pipeline.
/// </summary>
/// <remarks>
/// Sinks should be stateless and thread-safe.
/// Multiple sinks can be registered to fan out events.
/// </remarks>
public interface IWideEventSink
{
    /// <summary>
    /// Writes a wide event to the sink's destination.
    /// </summary>
    /// <param name="wideEvent">The wide event to write.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask WriteAsync(WideEvent wideEvent, CancellationToken ct = default);
}

/// <summary>
/// A sink that writes to multiple underlying sinks.
/// </summary>
public sealed class CompositeWideEventSink : IWideEventSink
{
    private readonly IWideEventSink[] _sinks;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeWideEventSink"/> class.
    /// </summary>
    /// <param name="sinks">The sinks to write to.</param>
    public CompositeWideEventSink(params IWideEventSink[] sinks)
    {
        _sinks = sinks;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeWideEventSink"/> class.
    /// </summary>
    /// <param name="sinks">The sinks to write to.</param>
    public CompositeWideEventSink(IEnumerable<IWideEventSink> sinks)
    {
        _sinks = sinks.ToArray();
    }

    /// <inheritdoc />
    public async ValueTask WriteAsync(WideEvent wideEvent, CancellationToken ct = default)
    {
        foreach (var sink in _sinks)
        {
            await sink.WriteAsync(wideEvent, ct).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// A sink that writes events to an in-memory list.
/// Useful for testing and debugging.
/// </summary>
public sealed class InMemoryWideEventSink : IWideEventSink
{
    private readonly List<WideEvent> _events = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets the events that have been written to this sink.
    /// </summary>
    public IReadOnlyList<WideEvent> Events
    {
        get
        {
            lock (_lock)
            {
                return _events.ToList();
            }
        }
    }

    /// <summary>
    /// Gets the count of events written to this sink.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _events.Count;
            }
        }
    }

    /// <inheritdoc />
    public ValueTask WriteAsync(WideEvent wideEvent, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _events.Add(wideEvent);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Clears all captured events.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }
}
