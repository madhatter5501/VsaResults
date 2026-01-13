namespace VsaResults.Messaging;

/// <summary>
/// Abstraction for emitting message processing wide events.
/// Implement this to integrate with your telemetry system (Serilog, OpenTelemetry, etc.).
/// </summary>
public interface IMessageWideEventEmitter
{
    /// <summary>
    /// Emits a wide event for message processing.
    /// </summary>
    /// <param name="wideEvent">The wide event to emit.</param>
    void Emit(MessageWideEvent wideEvent);
}

/// <summary>
/// A null implementation that discards all events.
/// Used when WideEvents are not configured.
/// </summary>
public sealed class NullMessageWideEventEmitter : IMessageWideEventEmitter
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly NullMessageWideEventEmitter Instance = new();

    private NullMessageWideEventEmitter()
    {
    }

    /// <inheritdoc />
    public void Emit(MessageWideEvent wideEvent)
    {
        // Intentionally empty - discards events
    }
}

/// <summary>
/// Adapter that bridges IWideEventEmitter and IMessageWideEventEmitter.
/// Converts MessageWideEvent to FeatureWideEvent format for unified logging.
/// </summary>
public sealed class WideEventEmitterAdapter : IMessageWideEventEmitter
{
    private readonly IWideEventEmitter _emitter;

    /// <summary>
    /// Initializes a new instance of the <see cref="WideEventEmitterAdapter"/> class.
    /// </summary>
    /// <param name="emitter">The underlying feature wide event emitter.</param>
    public WideEventEmitterAdapter(IWideEventEmitter emitter)
    {
        _emitter = emitter;
    }

    /// <inheritdoc />
    public void Emit(MessageWideEvent messageEvent)
    {
        // Convert to FeatureWideEvent for unified logging
        var featureEvent = new FeatureWideEvent
        {
            // Trace context
            TraceId = messageEvent.TraceId,
            SpanId = messageEvent.SpanId,
            ParentSpanId = messageEvent.ParentSpanId,
            Timestamp = messageEvent.Timestamp,

            // Map message processing to feature execution
            FeatureName = $"Message:{messageEvent.MessageType}",
            FeatureType = "Consumer",
            RequestType = messageEvent.MessageType,
            ResultType = "Unit",

            // Service context
            ServiceName = messageEvent.ServiceName,
            ServiceVersion = messageEvent.ServiceVersion,
            Environment = messageEvent.Environment,
            DeploymentId = messageEvent.DeploymentId,
            Region = messageEvent.Region,
            Host = messageEvent.Host,

            // Map consumer to mutator
            MutatorType = messageEvent.ConsumerType,

            // Timing
            ExecutionMs = messageEvent.ConsumerMs,
            TotalMs = messageEvent.TotalMs,

            // Outcome mapping
            Outcome = MapOutcome(messageEvent.Outcome),

            // Errors
            ErrorCode = messageEvent.ErrorCode,
            ErrorType = messageEvent.ErrorType,
            ErrorMessage = messageEvent.ErrorMessage,
            ErrorDescription = messageEvent.ErrorDescription,
            ErrorCount = messageEvent.ErrorCount,
            FailedAtStage = messageEvent.FailedAtStage,

            // Exception
            ExceptionType = messageEvent.ExceptionType,
            ExceptionMessage = messageEvent.ExceptionMessage,
            ExceptionStackTrace = messageEvent.ExceptionStackTrace,
        };

        // Copy message context to request context
        foreach (var (key, value) in messageEvent.MessageContext)
        {
            featureEvent.RequestContext[key] = value;
        }

        // Add messaging-specific context
        featureEvent.RequestContext["message_id"] = messageEvent.MessageId;
        featureEvent.RequestContext["correlation_id"] = messageEvent.CorrelationId;
        featureEvent.RequestContext["endpoint_name"] = messageEvent.EndpointName;
        featureEvent.RequestContext["retry_attempt"] = messageEvent.RetryAttempt;

        if (messageEvent.QueueTimeMs.HasValue)
        {
            featureEvent.RequestContext["queue_time_ms"] = messageEvent.QueueTimeMs;
        }

        if (messageEvent.FaultPublished)
        {
            featureEvent.RequestContext["fault_published"] = true;
            featureEvent.RequestContext["fault_message_id"] = messageEvent.FaultMessageId;
        }

        _emitter.Emit(featureEvent);
    }

    private static string MapOutcome(string messageOutcome) => messageOutcome switch
    {
        "success" => "success",
        "consumer_error" => "execution_failure",
        "deserialization_error" => "validation_failure",
        "retry_exhausted" => "execution_failure",
        "circuit_breaker_open" => "requirements_failure",
        "timeout" => "execution_failure",
        "exception" => "exception",
        _ => messageOutcome
    };
}
