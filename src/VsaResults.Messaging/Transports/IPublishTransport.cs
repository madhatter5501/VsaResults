namespace VsaResults.Messaging;

/// <summary>
/// Transport for publishing events to multiple subscribers.
/// </summary>
public interface IPublishTransport
{
    /// <summary>
    /// Publishes a message to all subscribers.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="envelope">The message envelope.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Unit on success, or an error.</returns>
    Task<ErrorOr<Unit>> PublishAsync<TMessage>(
        MessageEnvelope envelope,
        CancellationToken ct = default)
        where TMessage : class, IEvent;
}
