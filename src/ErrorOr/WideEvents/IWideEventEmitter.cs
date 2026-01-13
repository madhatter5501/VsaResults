namespace VsaResults;

/// <summary>
/// Abstraction for emitting wide events.
/// Implement this to integrate with your telemetry system (Serilog, OpenTelemetry, etc.).
/// </summary>
public interface IWideEventEmitter
{
    /// <summary>
    /// Emits a wide event for a feature execution.
    /// </summary>
    /// <param name="wideEvent">The wide event to emit.</param>
    void Emit(FeatureWideEvent wideEvent);
}
