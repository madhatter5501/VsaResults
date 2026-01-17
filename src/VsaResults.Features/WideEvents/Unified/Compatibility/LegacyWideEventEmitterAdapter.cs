namespace VsaResults.WideEvents;

/// <summary>
/// Adapter that wraps the legacy <see cref="IWideEventEmitter"/> interface
/// and routes events to the unified <see cref="IUnifiedWideEventEmitter"/>.
/// </summary>
/// <remarks>
/// Use this adapter to continue using the old FeatureWideEvent API
/// while benefiting from the new unified emission pipeline.
/// </remarks>
public sealed class LegacyWideEventEmitterAdapter : IWideEventEmitter
{
    private readonly IUnifiedWideEventEmitter _unifiedEmitter;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWideEventEmitterAdapter"/> class.
    /// </summary>
    /// <param name="unifiedEmitter">The unified emitter to route events to.</param>
    public LegacyWideEventEmitterAdapter(IUnifiedWideEventEmitter unifiedEmitter)
    {
        _unifiedEmitter = unifiedEmitter;
    }

    /// <summary>
    /// Converts a legacy FeatureWideEvent to a unified WideEvent.
    /// </summary>
    /// <param name="legacy">The legacy event to convert.</param>
    /// <returns>The converted unified event.</returns>
    public static WideEvent ConvertToUnified(FeatureWideEvent legacy)
    {
        var unified = new WideEvent
        {
            EventType = "feature",
            Timestamp = legacy.Timestamp,
            Outcome = legacy.Outcome,
            TotalMs = legacy.TotalMs,
            TraceId = legacy.TraceId,
            SpanId = legacy.SpanId,
            ParentSpanId = legacy.ParentSpanId,
            ServiceName = legacy.ServiceName,
            ServiceVersion = legacy.ServiceVersion,
            Environment = legacy.Environment,
            DeploymentId = legacy.DeploymentId,
            Region = legacy.Region,
            Host = legacy.Host,
            Feature = new WideEventFeatureSegment
            {
                FeatureName = legacy.FeatureName,
                FeatureType = legacy.FeatureType,
                RequestType = legacy.RequestType,
                ResultType = legacy.ResultType,
                ValidatorType = legacy.ValidatorType,
                RequirementsType = legacy.RequirementsType,
                MutatorType = legacy.MutatorType,
                QueryType = legacy.QueryType,
                SideEffectsType = legacy.SideEffectsType,
                HasCustomValidator = legacy.HasCustomValidator,
                HasCustomRequirements = legacy.HasCustomRequirements,
                HasCustomSideEffects = legacy.HasCustomSideEffects,
                ValidationMs = legacy.ValidationMs,
                RequirementsMs = legacy.RequirementsMs,
                ExecutionMs = legacy.ExecutionMs,
                SideEffectsMs = legacy.SideEffectsMs,
                LoadedEntities = new Dictionary<string, string>(legacy.LoadedEntities),
            },
        };

        // Copy error information
        if (!legacy.IsSuccess)
        {
            unified.Error = new WideEventErrorSegment
            {
                Code = legacy.ErrorCode,
                Type = legacy.ErrorType,
                Message = legacy.ErrorMessage,
                AllDescriptions = legacy.ErrorDescription,
                Count = legacy.ErrorCount ?? 0,
                FailedAtStage = legacy.FailedAtStage,
                FailedInNamespace = legacy.FailedInNamespace,
                FailedInClass = legacy.FailedInClass,
                FailedInMethod = legacy.FailedInMethod,
                ExceptionType = legacy.ExceptionType,
                ExceptionMessage = legacy.ExceptionMessage,
                ExceptionStackTrace = legacy.ExceptionStackTrace,
            };
        }

        // Copy context
        foreach (var (key, value) in legacy.RequestContext)
        {
            unified.Context[key] = value;
        }

        return unified;
    }

    /// <inheritdoc />
    public void Emit(FeatureWideEvent wideEvent)
    {
        var unified = ConvertToUnified(wideEvent);
        _unifiedEmitter.Emit(unified);
    }
}
