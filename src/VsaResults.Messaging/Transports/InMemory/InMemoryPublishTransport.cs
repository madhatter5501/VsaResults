using System.Collections.Concurrent;

namespace VsaResults.Messaging;

/// <summary>
/// In-memory publish transport.
/// Routes messages to exchanges based on message type.
/// </summary>
internal sealed class InMemoryPublishTransport : IPublishTransport
{
    private readonly ConcurrentDictionary<string, InMemoryExchange> _exchanges;
    private readonly ConcurrentDictionary<string, InMemoryQueue> _queues;

    public InMemoryPublishTransport(
        ConcurrentDictionary<string, InMemoryExchange> exchanges,
        ConcurrentDictionary<string, InMemoryQueue> queues)
    {
        _exchanges = exchanges;
        _queues = queues;
    }

    /// <inheritdoc />
    public async Task<ErrorOr<Unit>> PublishAsync<TMessage>(
        MessageEnvelope envelope,
        CancellationToken ct = default)
        where TMessage : class, IEvent
    {
        // Publish to all exchanges that match the message types
        foreach (var messageType in envelope.MessageTypes)
        {
            if (_exchanges.TryGetValue(messageType, out var exchange))
            {
                await exchange.PublishAsync(envelope, ct);
            }
        }

        return Unit.Value;
    }
}
