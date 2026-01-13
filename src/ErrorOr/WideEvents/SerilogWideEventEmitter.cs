using Microsoft.Extensions.Logging;

namespace VsaResults;

/// <summary>
/// Wide event emitter that uses ILogger (compatible with Serilog and other logging providers).
/// Logs the wide event as a structured log message at Information level for success,
/// or Warning level for failures.
/// </summary>
public sealed class SerilogWideEventEmitter : IWideEventEmitter
{
    private const string MessageTemplate =
        "FeatureWideEvent {FeatureName} {FeatureType} {Outcome} in {TotalMs:F2}ms " +
        "[Validation: {ValidationMs:F2}ms] [Requirements: {RequirementsMs:F2}ms] " +
        "[Execution: {ExecutionMs:F2}ms] [SideEffects: {SideEffectsMs:F2}ms] " +
        "TraceId={TraceId} SpanId={SpanId} " +
        "RequestType={RequestType} ResultType={ResultType} " +
        "ErrorCode={ErrorCode} ErrorType={ErrorType} FailedAtStage={FailedAtStage} " +
        "Service={ServiceName} Environment={Environment} Host={Host} " +
        "Context={@RequestContext} LoadedEntities={@LoadedEntities}";

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerilogWideEventEmitter"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for emitting events.</param>
    public SerilogWideEventEmitter(ILogger<SerilogWideEventEmitter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerilogWideEventEmitter"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for emitting events.</param>
    public SerilogWideEventEmitter(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Emits the wide event as a structured log message.
    /// </summary>
    /// <param name="wideEvent">The wide event to emit.</param>
    public void Emit(FeatureWideEvent wideEvent)
    {
        var logLevel = wideEvent.IsSuccess ? LogLevel.Information : LogLevel.Warning;

        var args = new object?[]
        {
            wideEvent.FeatureName,
            wideEvent.FeatureType,
            wideEvent.Outcome,
            wideEvent.TotalMs,
            wideEvent.ValidationMs ?? 0,
            wideEvent.RequirementsMs ?? 0,
            wideEvent.ExecutionMs ?? 0,
            wideEvent.SideEffectsMs ?? 0,
            wideEvent.TraceId,
            wideEvent.SpanId,
            wideEvent.RequestType,
            wideEvent.ResultType,
            wideEvent.ErrorCode,
            wideEvent.ErrorType,
            wideEvent.FailedAtStage,
            wideEvent.ServiceName,
            wideEvent.Environment,
            wideEvent.Host,
            wideEvent.RequestContext,
            wideEvent.LoadedEntities,
        };

        _logger.Log(logLevel, MessageTemplate, args);
    }
}
