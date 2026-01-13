namespace VsaResults.Messaging;

/// <summary>
/// The main message bus interface.
/// Combines publishing, sending, and bus control capabilities.
/// </summary>
public interface IBus : IPublishEndpoint, ISendEndpointProvider, IBusControl
{
    /// <summary>Gets the bus address.</summary>
    EndpointAddress Address { get; }
}
