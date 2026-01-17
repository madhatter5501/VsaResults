using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace VsaResults;

/// <summary>
/// Builder for constructing wide events throughout the feature lifecycle.
/// </summary>
public sealed partial class FeatureWideEventBuilder
{
    // Cached environment info - read once at first use, never changes during runtime
    private static readonly Lazy<EnvironmentInfo> CachedEnvironment = new(
        () => new EnvironmentInfo
        {
            ServiceName = System.Environment.GetEnvironmentVariable("SERVICE_NAME"),
            ServiceVersion = System.Environment.GetEnvironmentVariable("SERVICE_VERSION"),
            Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
            DeploymentId = System.Environment.GetEnvironmentVariable("DEPLOYMENT_ID"),
            Region = System.Environment.GetEnvironmentVariable("AZURE_REGION")
                ?? System.Environment.GetEnvironmentVariable("AWS_REGION"),
            Host = System.Environment.MachineName,
        },
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly ConcurrentDictionary<Type, IReadOnlyList<(PropertyInfo Property, WideEventPropertyAttribute Attribute)>> PropertyCache = new();

    private readonly FeatureWideEvent _event;
    private readonly Stopwatch _totalStopwatch;
    private readonly Stopwatch _stageStopwatch;
    private string _currentStage = "unknown";
    private string? _currentNamespace;
    private string? _currentClass;
    private string? _currentMethod;

    internal FeatureWideEventBuilder(string featureName, string featureType)
    {
        _totalStopwatch = Stopwatch.StartNew();
        _stageStopwatch = new Stopwatch();

        var activity = Activity.Current;
        var env = CachedEnvironment.Value;

        _event = new FeatureWideEvent
        {
            FeatureName = featureName,
            FeatureType = featureType,
            Outcome = "pending",
            TraceId = activity?.TraceId.ToString(),
            SpanId = activity?.SpanId.ToString(),
            ParentSpanId = activity?.ParentSpanId.ToString(),
            ServiceName = env.ServiceName,
            ServiceVersion = env.ServiceVersion,
            Environment = env.Environment,
            DeploymentId = env.DeploymentId,
            Region = env.Region,
            Host = env.Host,
        };
    }

    /// <summary>
    /// Records the request and result types for better queryability.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <returns>This builder for method chaining.</returns>
    public FeatureWideEventBuilder WithTypes<TRequest, TResult>()
    {
        _event.RequestType = typeof(TRequest).Name;
        _event.ResultType = typeof(TResult).Name;
        return this;
    }

    /// <summary>
    /// Adds custom context to the wide event (non-sensitive request data).
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    /// <returns>This builder for method chaining.</returns>
    public FeatureWideEventBuilder WithContext(string key, object? value)
    {
        _event.RequestContext[key] = value;
        return this;
    }

    /// <summary>
    /// Extracts context from a request object using [WideEventProperty] attributes
    /// and IWideEventContext interface.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request to extract context from.</param>
    /// <returns>This builder for method chaining.</returns>
    public FeatureWideEventBuilder WithRequestContext<TRequest>(TRequest request)
    {
        if (request == null)
        {
            return this;
        }

        // Extract from [WideEventProperty] attributes
        var properties = GetWideEventProperties(typeof(TRequest));
        foreach (var (prop, attr) in properties)
        {
            var value = prop.GetValue(request);
            var key = attr.Key ?? ToSnakeCase(prop.Name);
            _event.RequestContext[key] = value;
        }

        // Extract from IWideEventContext interface
        if (request is IWideEventContext contextProvider)
        {
            foreach (var (key, value) in contextProvider.GetWideEventContext())
            {
                _event.RequestContext[key] = value;
            }
        }

        return this;
    }

    /// <summary>
    /// Merges context from a FeatureContext's WideEventContext dictionary and loaded entities.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="context">The feature context to merge from.</param>
    /// <returns>This builder for method chaining.</returns>
    public FeatureWideEventBuilder WithFeatureContext<TRequest>(FeatureContext<TRequest>? context)
    {
        if (context == null)
        {
            return this;
        }

        // Merge wide event context
        foreach (var (key, value) in context.WideEventContext)
        {
            _event.RequestContext[key] = value;
        }

        // Capture loaded entities (type names only for non-PII)
        foreach (var (key, value) in context.Entities)
        {
            _event.LoadedEntities[key] = value?.GetType().Name ?? "null";
        }

        return this;
    }

    /// <summary>
    /// Merges context from an ErrorOr's Context dictionary.
    /// </summary>
    /// <param name="context">The context dictionary to merge.</param>
    /// <returns>This builder for method chaining.</returns>
    public FeatureWideEventBuilder WithResultContext(IReadOnlyDictionary<string, object> context)
    {
        foreach (var (key, value) in context)
        {
            _event.RequestContext[key] = value;
        }

        return this;
    }

    /// <summary>
    /// Records the pipeline stage types for debugging and analytics.
    /// </summary>
    /// <param name="validatorType">The validator type.</param>
    /// <param name="requirementsType">The requirements type.</param>
    /// <param name="mutatorOrQueryType">The mutator or query type.</param>
    /// <param name="sideEffectsType">The side effects type.</param>
    /// <param name="isMutation">Whether this is a mutation feature.</param>
    /// <returns>This builder for method chaining.</returns>
    public FeatureWideEventBuilder WithPipelineStages(
        Type? validatorType,
        Type? requirementsType,
        Type? mutatorOrQueryType,
        Type? sideEffectsType,
        bool isMutation)
    {
        if (validatorType != null)
        {
            _event.ValidatorType = validatorType.Name;
            _event.HasCustomValidator = !validatorType.Name.StartsWith("NoOpValidator", StringComparison.Ordinal);
        }

        if (requirementsType != null)
        {
            _event.RequirementsType = requirementsType.Name;
            _event.HasCustomRequirements = !requirementsType.Name.StartsWith("NoOpRequirements", StringComparison.Ordinal);
        }

        if (mutatorOrQueryType != null)
        {
            if (isMutation)
            {
                _event.MutatorType = mutatorOrQueryType.Name;
            }
            else
            {
                _event.QueryType = mutatorOrQueryType.Name;
            }
        }

        if (sideEffectsType != null)
        {
            _event.SideEffectsType = sideEffectsType.Name;
            _event.HasCustomSideEffects = !sideEffectsType.Name.StartsWith("NoOpSideEffects", StringComparison.Ordinal);
        }

        return this;
    }

    /// <summary>
    /// Starts timing a stage of the feature execution.
    /// </summary>
    public void StartStage() => _stageStopwatch.Restart();

    /// <summary>
    /// Starts timing a stage of the feature execution and records the stage name for exception tracking.
    /// </summary>
    /// <param name="stageName">The name of the stage being executed.</param>
    public void StartStage(string stageName)
    {
        _currentStage = stageName;
        _currentNamespace = null;
        _currentClass = null;
        _currentMethod = null;
        _stageStopwatch.Restart();
    }

    /// <summary>
    /// Starts timing a stage and records the component type being executed.
    /// </summary>
    /// <param name="stageName">The name of the stage being executed.</param>
    /// <param name="componentType">The type of the component being executed.</param>
    /// <param name="methodName">The method name being called on the component.</param>
    public void StartStage(string stageName, Type? componentType, string methodName)
    {
        _currentStage = stageName;
        _currentNamespace = componentType?.Namespace;
        _currentClass = componentType?.Name;
        _currentMethod = methodName;
        _stageStopwatch.Restart();
    }

    /// <summary>
    /// Records the duration of the validation stage.
    /// </summary>
    public void RecordValidation()
    {
        _event.ValidationMs = _stageStopwatch.Elapsed.TotalMilliseconds;
        _stageStopwatch.Reset();
    }

    /// <summary>
    /// Records the duration of the requirements stage.
    /// </summary>
    public void RecordRequirements()
    {
        _event.RequirementsMs = _stageStopwatch.Elapsed.TotalMilliseconds;
        _stageStopwatch.Reset();
    }

    /// <summary>
    /// Records the duration of the execution stage.
    /// </summary>
    public void RecordExecution()
    {
        _event.ExecutionMs = _stageStopwatch.Elapsed.TotalMilliseconds;
        _stageStopwatch.Reset();
    }

    /// <summary>
    /// Records the duration of the side effects stage.
    /// </summary>
    public void RecordSideEffects()
    {
        _event.SideEffectsMs = _stageStopwatch.Elapsed.TotalMilliseconds;
        _stageStopwatch.Reset();
    }

    /// <summary>
    /// Marks the feature execution as successful.
    /// </summary>
    /// <returns>The completed wide event.</returns>
    public FeatureWideEvent Success()
    {
        _event.Outcome = "success";
        return Build();
    }

    /// <summary>
    /// Marks the feature execution as failed due to validation.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>The completed wide event.</returns>
    public FeatureWideEvent ValidationFailure(IReadOnlyList<Error> errors)
    {
        _event.Outcome = "validation_failure";
        _event.FailedAtStage = "validation";
        PopulateFailureLocation();
        PopulateErrors(errors);
        return Build();
    }

    /// <summary>
    /// Marks the feature execution as failed due to requirements enforcement.
    /// </summary>
    /// <param name="errors">The requirements errors.</param>
    /// <returns>The completed wide event.</returns>
    public FeatureWideEvent RequirementsFailure(IReadOnlyList<Error> errors)
    {
        _event.Outcome = "requirements_failure";
        _event.FailedAtStage = "requirements";
        PopulateFailureLocation();
        PopulateErrors(errors);
        return Build();
    }

    /// <summary>
    /// Marks the feature execution as failed during execution.
    /// </summary>
    /// <param name="errors">The execution errors.</param>
    /// <returns>The completed wide event.</returns>
    public FeatureWideEvent ExecutionFailure(IReadOnlyList<Error> errors)
    {
        _event.Outcome = "execution_failure";
        _event.FailedAtStage = "execution";
        PopulateFailureLocation();
        PopulateErrors(errors);
        return Build();
    }

    /// <summary>
    /// Marks the feature execution as failed during side effects.
    /// </summary>
    /// <param name="errors">The side effects errors.</param>
    /// <returns>The completed wide event.</returns>
    public FeatureWideEvent SideEffectsFailure(IReadOnlyList<Error> errors)
    {
        _event.Outcome = "side_effects_failure";
        _event.FailedAtStage = "side_effects";
        PopulateFailureLocation();
        PopulateErrors(errors);
        return Build();
    }

    /// <summary>
    /// Marks the feature execution as failed due to an unhandled exception.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <returns>The completed wide event.</returns>
    public FeatureWideEvent Exception(Exception ex)
    {
        _event.Outcome = "exception";
        _event.FailedAtStage = _currentStage;
        PopulateFailureLocation();
        _event.ExceptionType = ex.GetType().FullName;
        _event.ExceptionMessage = ex.Message;
        _event.ExceptionStackTrace = ex.StackTrace;
        return Build();
    }

    [GeneratedRegex(@"([a-z0-9])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex SnakeCaseRegex();

    private static IReadOnlyList<(PropertyInfo Property, WideEventPropertyAttribute Attribute)> GetWideEventProperties(Type type)
    {
        return PropertyCache.GetOrAdd(type, t =>
        {
            var result = new List<(PropertyInfo Property, WideEventPropertyAttribute Attribute)>();
            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = prop.GetCustomAttribute<WideEventPropertyAttribute>();
                if (attr != null)
                {
                    result.Add((prop, attr));
                }
            }

            return result;
        });
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return SnakeCaseRegex().Replace(input, "$1_$2").ToLowerInvariant();
    }

    private void PopulateFailureLocation()
    {
        _event.FailedInNamespace = _currentNamespace;
        _event.FailedInClass = _currentClass;
        _event.FailedInMethod = _currentMethod;
    }

    private void PopulateErrors(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        var firstError = errors[0];
        _event.ErrorCode = firstError.Code;
        _event.ErrorType = firstError.Type.ToString();
        _event.ErrorMessage = firstError.Description;
        _event.ErrorCount = errors.Count;

        if (errors.Count > 1)
        {
            _event.ErrorDescription = string.Join("; ", errors.Select(e => $"{e.Code}: {e.Description}"));
        }
    }

    private FeatureWideEvent Build()
    {
        _totalStopwatch.Stop();
        _event.TotalMs = _totalStopwatch.Elapsed.TotalMilliseconds;
        return _event;
    }

    private sealed class EnvironmentInfo
    {
        public string? ServiceName { get; init; }
        public string? ServiceVersion { get; init; }
        public string? Environment { get; init; }
        public string? DeploymentId { get; init; }
        public string? Region { get; init; }
        public string? Host { get; init; }
    }
}
