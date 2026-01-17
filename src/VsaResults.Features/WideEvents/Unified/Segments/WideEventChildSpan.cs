namespace VsaResults.WideEvents;

/// <summary>
/// Represents a child operation captured within a parent wide event scope.
/// Used when <see cref="WideEventAggregationMode.AggregateToParent"/> is active.
/// </summary>
public sealed class WideEventChildSpan
{
    /// <summary>Gets or sets the unique identifier for this child span.</summary>
    public required string SpanId { get; set; }

    /// <summary>Gets or sets the type of event: "feature", "message", or custom type.</summary>
    public required string EventType { get; set; }

    /// <summary>Gets or sets the name of the operation (feature name, message type, etc.).</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets when this child operation started.</summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>Gets or sets the duration in milliseconds.</summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the outcome: success, validation_failure, requirements_failure,
    /// execution_failure, consumer_error, exception, etc.
    /// </summary>
    public required string Outcome { get; set; }

    /// <summary>Gets a value indicating whether this child operation was successful.</summary>
    public bool IsSuccess => Outcome == "success";

    /// <summary>Gets or sets error information if the child failed.</summary>
    public WideEventErrorSegment? Error { get; set; }

    /// <summary>Gets or sets the feature segment if this was a feature execution.</summary>
    public WideEventFeatureSegment? Feature { get; set; }

    /// <summary>Gets or sets context data specific to this child span.</summary>
    public Dictionary<string, object?>? Context { get; set; }

    /// <summary>
    /// Creates a child span from an existing WideEvent.
    /// </summary>
    /// <param name="wideEvent">The wide event to convert to a child span.</param>
    /// <returns>A child span representing the event.</returns>
    public static WideEventChildSpan FromWideEvent(WideEvent wideEvent)
    {
        return new WideEventChildSpan
        {
            SpanId = wideEvent.SpanId ?? Guid.NewGuid().ToString("N")[..16],
            EventType = wideEvent.EventType,
            Name = GetName(wideEvent),
            StartTime = wideEvent.Timestamp,
            DurationMs = wideEvent.TotalMs,
            Outcome = wideEvent.Outcome,
            Error = wideEvent.Error,
            Feature = wideEvent.Feature,
            Context = wideEvent.Context.Count > 0 ? new Dictionary<string, object?>(wideEvent.Context) : null,
        };
    }

    private static string GetName(WideEvent wideEvent)
    {
        if (wideEvent.Feature != null)
        {
            return wideEvent.Feature.FeatureName;
        }

        if (wideEvent.Message != null)
        {
            return wideEvent.Message.MessageType;
        }

        return wideEvent.EventType;
    }
}
