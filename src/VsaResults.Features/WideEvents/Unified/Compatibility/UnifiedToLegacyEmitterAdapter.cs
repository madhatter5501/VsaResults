namespace VsaResults.WideEvents;

/// <summary>
/// Adapter that wraps the unified <see cref="IUnifiedWideEventEmitter"/>
/// and exposes the legacy <see cref="IWideEventEmitter"/> interface.
/// </summary>
/// <remarks>
/// Use this adapter to use the new unified system with code that
/// expects the old IWideEventEmitter interface.
/// </remarks>
public sealed class UnifiedToLegacyEmitterAdapter : IWideEventEmitter
{
    private readonly IUnifiedWideEventEmitter _unifiedEmitter;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedToLegacyEmitterAdapter"/> class.
    /// </summary>
    /// <param name="unifiedEmitter">The unified emitter to wrap.</param>
    public UnifiedToLegacyEmitterAdapter(IUnifiedWideEventEmitter unifiedEmitter)
    {
        _unifiedEmitter = unifiedEmitter;
    }

    /// <inheritdoc />
    public void Emit(FeatureWideEvent wideEvent)
    {
        var unified = LegacyWideEventEmitterAdapter.ConvertToUnified(wideEvent);
        _unifiedEmitter.Emit(unified);
    }
}
