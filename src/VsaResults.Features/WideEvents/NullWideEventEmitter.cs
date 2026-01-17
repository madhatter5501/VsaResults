namespace VsaResults;

/// <summary>
/// A no-op emitter that discards wide events.
/// Useful for testing or when telemetry is disabled.
/// </summary>
public sealed class NullWideEventEmitter : IWideEventEmitter
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static readonly NullWideEventEmitter Instance = new();

    private NullWideEventEmitter()
    {
    }

    /// <summary>
    /// Does nothing with the wide event.
    /// </summary>
    /// <param name="wideEvent">The wide event to discard.</param>
    public void Emit(FeatureWideEvent wideEvent)
    {
        // Intentionally empty
    }
}
