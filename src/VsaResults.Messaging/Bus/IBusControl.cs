namespace VsaResults.Messaging;

/// <summary>
/// Controls the bus lifecycle.
/// </summary>
public interface IBusControl
{
    /// <summary>
    /// Starts the bus and begins receiving messages.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Unit on success, or an error.</returns>
    Task<ErrorOr<Unit>> StartAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops the bus gracefully, allowing in-flight messages to complete.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Unit on success, or an error.</returns>
    Task<ErrorOr<Unit>> StopAsync(CancellationToken ct = default);
}
