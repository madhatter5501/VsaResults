namespace VsaResults.WideEvents;

/// <summary>
/// Sink adapter that writes unified events to a legacy IWideEventEmitter.
/// </summary>
/// <remarks>
/// Use this to integrate the unified system with existing IWideEventEmitter implementations.
/// Note: Only feature-type events can be converted back to FeatureWideEvent.
/// </remarks>
public sealed class LegacyEmitterSinkAdapter : IWideEventSink
{
    private readonly IWideEventEmitter _legacyEmitter;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyEmitterSinkAdapter"/> class.
    /// </summary>
    /// <param name="legacyEmitter">The legacy emitter to write to.</param>
    public LegacyEmitterSinkAdapter(IWideEventEmitter legacyEmitter)
    {
        _legacyEmitter = legacyEmitter;
    }

    /// <summary>
    /// Converts a unified WideEvent back to a legacy FeatureWideEvent.
    /// </summary>
    /// <param name="unified">The unified event to convert.</param>
    /// <returns>The converted legacy event.</returns>
    public static FeatureWideEvent ConvertToLegacy(WideEvent unified)
    {
        var legacy = new FeatureWideEvent
        {
            FeatureName = unified.Feature?.FeatureName ?? unified.Message?.MessageType ?? unified.EventType,
            FeatureType = unified.Feature?.FeatureType ?? (unified.Message != null ? "Consumer" : unified.EventType),
            Outcome = unified.Outcome,
            TotalMs = unified.TotalMs,
            Timestamp = unified.Timestamp,
            TraceId = unified.TraceId,
            SpanId = unified.SpanId,
            ParentSpanId = unified.ParentSpanId,
            ServiceName = unified.ServiceName,
            ServiceVersion = unified.ServiceVersion,
            Environment = unified.Environment,
            DeploymentId = unified.DeploymentId,
            Region = unified.Region,
            Host = unified.Host,
        };

        // Copy feature segment
        if (unified.Feature != null)
        {
            legacy.RequestType = unified.Feature.RequestType;
            legacy.ResultType = unified.Feature.ResultType;
            legacy.ValidatorType = unified.Feature.ValidatorType;
            legacy.RequirementsType = unified.Feature.RequirementsType;
            legacy.MutatorType = unified.Feature.MutatorType;
            legacy.QueryType = unified.Feature.QueryType;
            legacy.SideEffectsType = unified.Feature.SideEffectsType;
            legacy.HasCustomValidator = unified.Feature.HasCustomValidator;
            legacy.HasCustomRequirements = unified.Feature.HasCustomRequirements;
            legacy.HasCustomSideEffects = unified.Feature.HasCustomSideEffects;
            legacy.ValidationMs = unified.Feature.ValidationMs;
            legacy.RequirementsMs = unified.Feature.RequirementsMs;
            legacy.ExecutionMs = unified.Feature.ExecutionMs;
            legacy.SideEffectsMs = unified.Feature.SideEffectsMs;

            foreach (var (key, value) in unified.Feature.LoadedEntities)
            {
                legacy.LoadedEntities[key] = value;
            }
        }

        // Copy error segment
        if (unified.Error != null)
        {
            legacy.ErrorCode = unified.Error.Code;
            legacy.ErrorType = unified.Error.Type;
            legacy.ErrorMessage = unified.Error.Message;
            legacy.ErrorDescription = unified.Error.AllDescriptions;
            legacy.ErrorCount = unified.Error.Count;
            legacy.FailedAtStage = unified.Error.FailedAtStage;
            legacy.FailedInNamespace = unified.Error.FailedInNamespace;
            legacy.FailedInClass = unified.Error.FailedInClass;
            legacy.FailedInMethod = unified.Error.FailedInMethod;
            legacy.ExceptionType = unified.Error.ExceptionType;
            legacy.ExceptionMessage = unified.Error.ExceptionMessage;
            legacy.ExceptionStackTrace = unified.Error.ExceptionStackTrace;
        }

        // Copy context
        foreach (var (key, value) in unified.Context)
        {
            legacy.RequestContext[key] = value;
        }

        // Add message-specific context if present
        if (unified.Message != null)
        {
            legacy.RequestContext["message_id"] = unified.Message.MessageId;
            legacy.RequestContext["message_type"] = unified.Message.MessageType;
            legacy.RequestContext["consumer_type"] = unified.Message.ConsumerType;
            legacy.RequestContext["endpoint_name"] = unified.Message.EndpointName;
            legacy.RequestContext["retry_attempt"] = unified.Message.RetryAttempt;

            if (unified.Message.QueueTimeMs.HasValue)
            {
                legacy.RequestContext["queue_time_ms"] = unified.Message.QueueTimeMs;
            }

            if (unified.Message.FaultPublished)
            {
                legacy.RequestContext["fault_published"] = true;
                legacy.RequestContext["fault_message_id"] = unified.Message.FaultMessageId;
            }
        }

        // Add correlation context
        if (unified.CorrelationId != null)
        {
            legacy.RequestContext["correlation_id"] = unified.CorrelationId;
        }

        if (unified.ConversationId != null)
        {
            legacy.RequestContext["conversation_id"] = unified.ConversationId;
        }

        if (unified.CausationId != null)
        {
            legacy.RequestContext["causation_id"] = unified.CausationId;
        }

        // Add child span summary
        if (unified.ChildSpans.Count > 0)
        {
            legacy.RequestContext["child_span_count"] = unified.ChildSpans.Count;
        }

        return legacy;
    }

    /// <inheritdoc />
    public ValueTask WriteAsync(WideEvent wideEvent, CancellationToken ct = default)
    {
        var legacy = ConvertToLegacy(wideEvent);
        _legacyEmitter.Emit(legacy);
        return ValueTask.CompletedTask;
    }
}
