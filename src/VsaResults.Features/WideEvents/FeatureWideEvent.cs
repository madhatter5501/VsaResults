namespace VsaResults;

/// <summary>
/// Wide Event for feature execution - a single comprehensive log event
/// emitted per feature execution containing all context needed for debugging.
///
/// Based on the "Canonical Log Lines" / "Wide Events" pattern:
/// https://loggingsucks.com/
///
/// Key principles:
/// - One event per feature execution (not scattered log lines)
/// - High cardinality fields (user_id, trace_id, feature_name)
/// - High dimensionality (many fields for rich querying)
/// - Build throughout, emit once at the end.
/// </summary>
public sealed class FeatureWideEvent
{
    // Request Context

    /// <summary>Gets or sets the distributed trace ID.</summary>
    public string? TraceId { get; set; }

    /// <summary>Gets or sets the current span ID.</summary>
    public string? SpanId { get; set; }

    /// <summary>Gets or sets the parent span ID.</summary>
    public string? ParentSpanId { get; set; }

    /// <summary>Gets or sets the event timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    // Feature Context

    /// <summary>Gets or sets the name of the feature being executed.</summary>
    public required string FeatureName { get; set; }

    /// <summary>Gets or sets the type of feature: "Mutation" or "Query".</summary>
    public required string FeatureType { get; set; }

    /// <summary>Gets or sets the name of the request type.</summary>
    public string? RequestType { get; set; }

    /// <summary>Gets or sets the name of the result type.</summary>
    public string? ResultType { get; set; }

    // Service Context

    /// <summary>Gets or sets the name of the service.</summary>
    public string? ServiceName { get; set; }

    /// <summary>Gets or sets the version of the service.</summary>
    public string? ServiceVersion { get; set; }

    /// <summary>Gets or sets the git commit hash for deployment correlation.</summary>
    public string? CommitHash { get; set; }

    /// <summary>Gets or sets the environment name (production, staging, etc.).</summary>
    public string? Environment { get; set; }

    /// <summary>Gets or sets the deployment identifier.</summary>
    public string? DeploymentId { get; set; }

    /// <summary>Gets or sets the region/datacenter.</summary>
    public string? Region { get; set; }

    /// <summary>Gets or sets the host machine name.</summary>
    public string? Host { get; set; }

    // Pipeline Stage Metadata

    /// <summary>Gets or sets the type name of the validator used.</summary>
    public string? ValidatorType { get; set; }

    /// <summary>Gets or sets the type name of the requirements enforcer used.</summary>
    public string? RequirementsType { get; set; }

    /// <summary>Gets or sets the type name of the mutator used.</summary>
    public string? MutatorType { get; set; }

    /// <summary>Gets or sets the type name of the query used.</summary>
    public string? QueryType { get; set; }

    /// <summary>Gets or sets the type name of the side effects handler used.</summary>
    public string? SideEffectsType { get; set; }

    /// <summary>Gets or sets a value indicating whether a custom validator was used (vs NoOp).</summary>
    public bool HasCustomValidator { get; set; }

    /// <summary>Gets or sets a value indicating whether custom requirements were used (vs NoOp).</summary>
    public bool HasCustomRequirements { get; set; }

    /// <summary>Gets or sets a value indicating whether custom side effects were used (vs NoOp).</summary>
    public bool HasCustomSideEffects { get; set; }

    // Timing Breakdown (milliseconds)

    /// <summary>Gets or sets the time spent in validation stage.</summary>
    public double? ValidationMs { get; set; }

    /// <summary>Gets or sets the time spent in requirements stage.</summary>
    public double? RequirementsMs { get; set; }

    /// <summary>Gets or sets the time spent in execution stage.</summary>
    public double? ExecutionMs { get; set; }

    /// <summary>Gets or sets the time spent in side effects stage.</summary>
    public double? SideEffectsMs { get; set; }

    /// <summary>Gets or sets the total execution time.</summary>
    public double TotalMs { get; set; }

    // Outcome

    /// <summary>
    /// Gets or sets the execution outcome: success, validation_failure, requirements_failure,
    /// execution_failure, side_effects_failure, or exception.
    /// </summary>
    public required string Outcome { get; set; }

    /// <summary>Gets a value indicating whether the execution was successful.</summary>
    public bool IsSuccess => Outcome == "success";

    // Entities loaded during requirements

    /// <summary>Gets or sets the type names of entities loaded during requirements stage.</summary>
    public Dictionary<string, string> LoadedEntities { get; set; } = new();

    // Error Context (populated on failure)

    /// <summary>Gets or sets the first error code.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Gets or sets the first error type.</summary>
    public string? ErrorType { get; set; }

    /// <summary>Gets or sets the first error message.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets all error descriptions joined.</summary>
    public string? ErrorDescription { get; set; }

    /// <summary>Gets or sets the total number of errors.</summary>
    public int? ErrorCount { get; set; }

    /// <summary>Gets or sets which pipeline stage failed.</summary>
    public string? FailedAtStage { get; set; }

    /// <summary>Gets or sets the namespace of the component that was executing when failure occurred.</summary>
    public string? FailedInNamespace { get; set; }

    /// <summary>Gets or sets the class name of the component that was executing when failure occurred.</summary>
    public string? FailedInClass { get; set; }

    /// <summary>Gets or sets the method name that was executing when failure occurred.</summary>
    public string? FailedInMethod { get; set; }

    // Exception Context (populated on unhandled exception)

    /// <summary>Gets or sets the exception type name.</summary>
    public string? ExceptionType { get; set; }

    /// <summary>Gets or sets the exception message.</summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>Gets or sets the exception stack trace.</summary>
    public string? ExceptionStackTrace { get; set; }

    // Request Summary (non-sensitive fields from the request)

    /// <summary>Gets or sets the business context accumulated during execution.</summary>
    public Dictionary<string, object?> RequestContext { get; set; } = new();

    /// <summary>
    /// Creates a new wide event builder for a feature execution.
    /// </summary>
    /// <param name="featureName">The name of the feature.</param>
    /// <param name="featureType">The type of feature ("Mutation" or "Query").</param>
    /// <returns>A builder for constructing the wide event.</returns>
    public static FeatureWideEventBuilder Start(string featureName, string featureType)
        => new(featureName, featureType);
}
