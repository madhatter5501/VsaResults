using System.Diagnostics;
using VsaResults.WideEvents;

namespace VsaResults;

/// <summary>
/// Extension methods for executing features with automatic wide event emission.
/// </summary>
public static class FeatureExecutionExtensions
{
    private static readonly ActivitySource FeatureActivitySource = new("VsaResults.Features");

    /// <summary>
    /// Executes a mutation feature through the full pipeline:
    /// Validate → Enforce Requirements → Execute Mutation → Run Side Effects → Emit Wide Event.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="feature">The feature to execute.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="emitter">The wide event emitter for observability.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result or errors from execution.</returns>
    public static async Task<VsaResult<TResult>> ExecuteAsync<TRequest, TResult>(
        this IMutationFeature<TRequest, TResult> feature,
        TRequest request,
        IWideEventEmitter emitter,
        CancellationToken ct = default)
    {
        var featureName = feature.GetType().DeclaringType?.Name ?? feature.GetType().Name;
        using var activity = StartFeatureActivity<TRequest, TResult>(featureName, FeatureTypes.Mutation);
        var wideEvent = FeatureWideEvent.Start(featureName, FeatureTypes.Mutation)
            .WithTypes<TRequest, TResult>()
            .WithRequestContext(request)
            .WithPipelineStages(
                feature.Validator?.GetType(),
                feature.Requirements?.GetType(),
                feature.Mutator?.GetType(),
                feature.SideEffects?.GetType(),
                isMutation: true);

        FeatureContext<TRequest>? context = null;

        try
        {
            // Validation stage
            var validator = feature.Validator ?? NoOpValidator<TRequest>.Instance;
            wideEvent.StartStage(StageNames.Validation, validator.GetType(), MethodNames.ValidateAsync);
            var (validated, validationMs) = await MeasureAsync(() => validator.ValidateAsync(request, ct)).ConfigureAwait(false);
            wideEvent.RecordValidation();
            RecordStageEvent(activity, StageNames.Validation, validationMs);

            if (validated.IsError)
            {
                wideEvent.WithResultContext(validated.Context);
                RecordOutcome(activity, OutcomeNames.ValidationFailure);
                emitter.Emit(wideEvent.WithFeatureContext(context).ValidationFailure(validated.Errors));
                return new VsaResult<TResult>(validated.Errors, validated._context);
            }

            // Requirements stage
            var requirements = feature.Requirements ?? NoOpRequirements<TRequest>.Instance;
            wideEvent.StartStage(StageNames.Requirements, requirements.GetType(), MethodNames.EnforceAsync);
            var (enforced, requirementsMs) = await MeasureAsync(() => requirements.EnforceAsync(validated.Value, ct)).ConfigureAwait(false);
            wideEvent.RecordRequirements();
            RecordStageEvent(activity, StageNames.Requirements, requirementsMs);

            if (enforced.IsError)
            {
                wideEvent.WithResultContext(enforced.Context);
                RecordOutcome(activity, OutcomeNames.RequirementsFailure);
                emitter.Emit(wideEvent.WithFeatureContext(context).RequirementsFailure(enforced.Errors));
                return new VsaResult<TResult>(enforced.Errors, enforced._context);
            }

            context = enforced.Value;

            // Enrich with request metadata (IP, user agent, etc.)
            EnrichWithRequestMetadata(context);

            // Execution stage
            var mutator = feature.Mutator ?? throw new InvalidOperationException(ExceptionMessages.MutatorRequired);
            wideEvent.StartStage(StageNames.Execution, mutator.GetType(), MethodNames.ExecuteAsync);
            var (result, executionMs) = await MeasureAsync(() => mutator.ExecuteAsync(context, ct)).ConfigureAwait(false);
            wideEvent.RecordExecution();
            RecordStageEvent(activity, StageNames.Execution, executionMs);

            if (result.IsError)
            {
                wideEvent.WithResultContext(result.Context);
                RecordOutcome(activity, OutcomeNames.ExecutionFailure);
                emitter.Emit(wideEvent.WithFeatureContext(context).ExecutionFailure(result.Errors));
                return result;
            }

            // Side effects stage
            var sideEffects = feature.SideEffects ?? NoOpSideEffects<TRequest>.Instance;
            wideEvent.StartStage(StageNames.SideEffects, sideEffects.GetType(), MethodNames.ExecuteAsync);
            var (effects, sideEffectsMs) = await MeasureAsync(() => sideEffects.ExecuteAsync(context, ct)).ConfigureAwait(false);
            wideEvent.RecordSideEffects();
            RecordStageEvent(activity, StageNames.SideEffects, sideEffectsMs);

            if (effects.IsError)
            {
                wideEvent.WithResultContext(effects.Context);
                RecordOutcome(activity, OutcomeNames.SideEffectsFailure);
                emitter.Emit(wideEvent.WithFeatureContext(context).SideEffectsFailure(effects.Errors));
                return new VsaResult<TResult>(effects.Errors, effects._context);
            }

            wideEvent.WithResultContext(result.Context);
            RecordOutcome(activity, OutcomeNames.Success);
            emitter.Emit(wideEvent.WithFeatureContext(context).Success());
            return result;
        }
        catch (Exception ex)
        {
            RecordException(activity, ex);
            emitter.Emit(wideEvent.WithFeatureContext(context).Exception(ex));
            throw;
        }
    }

    /// <summary>
    /// Executes a query feature through the pipeline:
    /// Validate → Enforce Requirements → Execute Query → Emit Wide Event.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="feature">The feature to execute.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="emitter">The wide event emitter for observability.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result or errors from execution.</returns>
    public static async Task<VsaResult<TResult>> ExecuteAsync<TRequest, TResult>(
        this IQueryFeature<TRequest, TResult> feature,
        TRequest request,
        IWideEventEmitter emitter,
        CancellationToken ct = default)
    {
        var featureName = feature.GetType().DeclaringType?.Name ?? feature.GetType().Name;
        using var activity = StartFeatureActivity<TRequest, TResult>(featureName, FeatureTypes.Query);
        var wideEvent = FeatureWideEvent.Start(featureName, FeatureTypes.Query)
            .WithTypes<TRequest, TResult>()
            .WithRequestContext(request)
            .WithPipelineStages(
                feature.Validator?.GetType(),
                feature.Requirements?.GetType(),
                feature.Query?.GetType(),
                sideEffectsType: null,
                isMutation: false);

        FeatureContext<TRequest>? context = null;

        try
        {
            // Validation stage
            var validator = feature.Validator ?? NoOpValidator<TRequest>.Instance;
            wideEvent.StartStage(StageNames.Validation, validator.GetType(), MethodNames.ValidateAsync);
            var (validated, validationMs) = await MeasureAsync(() => validator.ValidateAsync(request, ct)).ConfigureAwait(false);
            wideEvent.RecordValidation();
            RecordStageEvent(activity, StageNames.Validation, validationMs);

            if (validated.IsError)
            {
                wideEvent.WithResultContext(validated.Context);
                RecordOutcome(activity, OutcomeNames.ValidationFailure);
                emitter.Emit(wideEvent.WithFeatureContext(context).ValidationFailure(validated.Errors));
                return new VsaResult<TResult>(validated.Errors, validated._context);
            }

            // Requirements stage
            var requirements = feature.Requirements ?? NoOpRequirements<TRequest>.Instance;
            wideEvent.StartStage(StageNames.Requirements, requirements.GetType(), MethodNames.EnforceAsync);
            var (enforced, requirementsMs) = await MeasureAsync(() => requirements.EnforceAsync(validated.Value, ct)).ConfigureAwait(false);
            wideEvent.RecordRequirements();
            RecordStageEvent(activity, StageNames.Requirements, requirementsMs);

            if (enforced.IsError)
            {
                wideEvent.WithResultContext(enforced.Context);
                RecordOutcome(activity, OutcomeNames.RequirementsFailure);
                emitter.Emit(wideEvent.WithFeatureContext(context).RequirementsFailure(enforced.Errors));
                return new VsaResult<TResult>(enforced.Errors, enforced._context);
            }

            context = enforced.Value;

            // Enrich with request metadata (IP, user agent, etc.)
            EnrichWithRequestMetadata(context);

            // Execution stage
            var query = feature.Query ?? throw new InvalidOperationException(ExceptionMessages.QueryRequired);
            wideEvent.StartStage(StageNames.Execution, query.GetType(), MethodNames.ExecuteAsync);
            var (result, executionMs) = await MeasureAsync(() => query.ExecuteAsync(context, ct)).ConfigureAwait(false);
            wideEvent.RecordExecution();
            RecordStageEvent(activity, StageNames.Execution, executionMs);

            if (result.IsError)
            {
                wideEvent.WithResultContext(result.Context);
                RecordOutcome(activity, OutcomeNames.ExecutionFailure);
                emitter.Emit(wideEvent.WithFeatureContext(context).ExecutionFailure(result.Errors));
                return result;
            }

            wideEvent.WithResultContext(result.Context);
            RecordOutcome(activity, OutcomeNames.Success);
            emitter.Emit(wideEvent.WithFeatureContext(context).Success());
            return result;
        }
        catch (Exception ex)
        {
            RecordException(activity, ex);
            emitter.Emit(wideEvent.WithFeatureContext(context).Exception(ex));
            throw;
        }
    }

    /// <summary>
    /// Executes a mutation feature using the unified wide events system.
    /// Automatically integrates with <see cref="WideEventScope"/> for aggregation.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="feature">The feature to execute.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="emitter">The unified wide event emitter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result or errors from execution.</returns>
    public static async Task<VsaResult<TResult>> ExecuteAsync<TRequest, TResult>(
        this IMutationFeature<TRequest, TResult> feature,
        TRequest request,
        IUnifiedWideEventEmitter emitter,
        CancellationToken ct = default)
    {
        var featureName = feature.GetType().DeclaringType?.Name ?? feature.GetType().Name;
        using var activity = StartFeatureActivity<TRequest, TResult>(featureName, FeatureTypes.Mutation);
        var builder = WideEvent.StartFeature(featureName, FeatureTypes.Mutation)
            .WithTypes<TRequest, TResult>()
            .WithRequestContext(request)
            .WithPipelineStages(
                feature.Validator?.GetType(),
                feature.Requirements?.GetType(),
                feature.Mutator?.GetType(),
                feature.SideEffects?.GetType(),
                isMutation: true);

        FeatureContext<TRequest>? context = null;

        try
        {
            // Validation stage
            var validator = feature.Validator ?? NoOpValidator<TRequest>.Instance;
            builder.StartStage(StageNames.Validation, validator.GetType(), MethodNames.ValidateAsync);
            var (validated, validationMs) = await MeasureAsync(() => validator.ValidateAsync(request, ct)).ConfigureAwait(false);
            builder.RecordValidation();
            RecordStageEvent(activity, StageNames.Validation, validationMs);

            if (validated.IsError)
            {
                builder.WithResultContext(validated.Context);
                RecordOutcome(activity, OutcomeNames.ValidationFailure);
                emitter.Emit(builder.WithFeatureContext(context).ValidationFailure(validated.Errors));
                return new VsaResult<TResult>(validated.Errors, validated._context);
            }

            // Requirements stage
            var requirements = feature.Requirements ?? NoOpRequirements<TRequest>.Instance;
            builder.StartStage(StageNames.Requirements, requirements.GetType(), MethodNames.EnforceAsync);
            var (enforced, requirementsMs) = await MeasureAsync(() => requirements.EnforceAsync(validated.Value, ct)).ConfigureAwait(false);
            builder.RecordRequirements();
            RecordStageEvent(activity, StageNames.Requirements, requirementsMs);

            if (enforced.IsError)
            {
                builder.WithResultContext(enforced.Context);
                RecordOutcome(activity, OutcomeNames.RequirementsFailure);
                emitter.Emit(builder.WithFeatureContext(context).RequirementsFailure(enforced.Errors));
                return new VsaResult<TResult>(enforced.Errors, enforced._context);
            }

            context = enforced.Value;

            // Enrich with request metadata (IP, user agent, etc.)
            EnrichWithRequestMetadata(context);

            // Execution stage
            var mutator = feature.Mutator ?? throw new InvalidOperationException(ExceptionMessages.MutatorRequired);
            builder.StartStage(StageNames.Execution, mutator.GetType(), MethodNames.ExecuteAsync);
            var (result, executionMs) = await MeasureAsync(() => mutator.ExecuteAsync(context, ct)).ConfigureAwait(false);
            builder.RecordExecution();
            RecordStageEvent(activity, StageNames.Execution, executionMs);

            if (result.IsError)
            {
                builder.WithResultContext(result.Context);
                RecordOutcome(activity, OutcomeNames.ExecutionFailure);
                emitter.Emit(builder.WithFeatureContext(context).ExecutionFailure(result.Errors));
                return result;
            }

            // Side effects stage
            var sideEffects = feature.SideEffects ?? NoOpSideEffects<TRequest>.Instance;
            builder.StartStage(StageNames.SideEffects, sideEffects.GetType(), MethodNames.ExecuteAsync);
            var (effects, sideEffectsMs) = await MeasureAsync(() => sideEffects.ExecuteAsync(context, ct)).ConfigureAwait(false);
            builder.RecordSideEffects();
            RecordStageEvent(activity, StageNames.SideEffects, sideEffectsMs);

            if (effects.IsError)
            {
                builder.WithResultContext(effects.Context);
                RecordOutcome(activity, OutcomeNames.SideEffectsFailure);
                emitter.Emit(builder.WithFeatureContext(context).SideEffectsFailure(effects.Errors));
                return new VsaResult<TResult>(effects.Errors, effects._context);
            }

            builder.WithResultContext(result.Context);
            RecordOutcome(activity, OutcomeNames.Success);
            emitter.Emit(builder.WithFeatureContext(context).Success());
            return result;
        }
        catch (Exception ex)
        {
            RecordException(activity, ex);
            emitter.Emit(builder.WithFeatureContext(context).Exception(ex));
            throw;
        }
    }

    /// <summary>
    /// Executes a query feature using the unified wide events system.
    /// Automatically integrates with <see cref="WideEventScope"/> for aggregation.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="feature">The feature to execute.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="emitter">The unified wide event emitter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result or errors from execution.</returns>
    public static async Task<VsaResult<TResult>> ExecuteAsync<TRequest, TResult>(
        this IQueryFeature<TRequest, TResult> feature,
        TRequest request,
        IUnifiedWideEventEmitter emitter,
        CancellationToken ct = default)
    {
        var featureName = feature.GetType().DeclaringType?.Name ?? feature.GetType().Name;
        using var activity = StartFeatureActivity<TRequest, TResult>(featureName, FeatureTypes.Query);
        var builder = WideEvent.StartFeature(featureName, FeatureTypes.Query)
            .WithTypes<TRequest, TResult>()
            .WithRequestContext(request)
            .WithPipelineStages(
                feature.Validator?.GetType(),
                feature.Requirements?.GetType(),
                feature.Query?.GetType(),
                sideEffectsType: null,
                isMutation: false);

        FeatureContext<TRequest>? context = null;

        try
        {
            // Validation stage
            var validator = feature.Validator ?? NoOpValidator<TRequest>.Instance;
            builder.StartStage(StageNames.Validation, validator.GetType(), MethodNames.ValidateAsync);
            var (validated, validationMs) = await MeasureAsync(() => validator.ValidateAsync(request, ct)).ConfigureAwait(false);
            builder.RecordValidation();
            RecordStageEvent(activity, StageNames.Validation, validationMs);

            if (validated.IsError)
            {
                builder.WithResultContext(validated.Context);
                RecordOutcome(activity, OutcomeNames.ValidationFailure);
                emitter.Emit(builder.WithFeatureContext(context).ValidationFailure(validated.Errors));
                return new VsaResult<TResult>(validated.Errors, validated._context);
            }

            // Requirements stage
            var requirements = feature.Requirements ?? NoOpRequirements<TRequest>.Instance;
            builder.StartStage(StageNames.Requirements, requirements.GetType(), MethodNames.EnforceAsync);
            var (enforced, requirementsMs) = await MeasureAsync(() => requirements.EnforceAsync(validated.Value, ct)).ConfigureAwait(false);
            builder.RecordRequirements();
            RecordStageEvent(activity, StageNames.Requirements, requirementsMs);

            if (enforced.IsError)
            {
                builder.WithResultContext(enforced.Context);
                RecordOutcome(activity, OutcomeNames.RequirementsFailure);
                emitter.Emit(builder.WithFeatureContext(context).RequirementsFailure(enforced.Errors));
                return new VsaResult<TResult>(enforced.Errors, enforced._context);
            }

            context = enforced.Value;

            // Enrich with request metadata (IP, user agent, etc.)
            EnrichWithRequestMetadata(context);

            // Execution stage
            var query = feature.Query ?? throw new InvalidOperationException(ExceptionMessages.QueryRequired);
            builder.StartStage(StageNames.Execution, query.GetType(), MethodNames.ExecuteAsync);
            var (result, executionMs) = await MeasureAsync(() => query.ExecuteAsync(context, ct)).ConfigureAwait(false);
            builder.RecordExecution();
            RecordStageEvent(activity, StageNames.Execution, executionMs);

            if (result.IsError)
            {
                builder.WithResultContext(result.Context);
                RecordOutcome(activity, OutcomeNames.ExecutionFailure);
                emitter.Emit(builder.WithFeatureContext(context).ExecutionFailure(result.Errors));
                return result;
            }

            builder.WithResultContext(result.Context);
            RecordOutcome(activity, OutcomeNames.Success);
            emitter.Emit(builder.WithFeatureContext(context).Success());
            return result;
        }
        catch (Exception ex)
        {
            RecordException(activity, ex);
            emitter.Emit(builder.WithFeatureContext(context).Exception(ex));
            throw;
        }
    }

    /// <summary>
    /// Enriches the feature context with request metadata from the current scope.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="context">The feature context to enrich.</param>
    private static void EnrichWithRequestMetadata<TRequest>(FeatureContext<TRequest> context)
    {
        var provider = RequestMetadataScope.Current;
        if (provider is null)
        {
            return;
        }

        if (provider.IpAddress is { } ip)
        {
            context.AddContext("ip_address", ip);
        }

        if (provider.UserAgent is { } ua)
        {
            context.AddContext("user_agent", ua);
        }

        if (provider.TraceId is { } traceId)
        {
            context.AddContext("request_id", traceId);
        }

        if (provider.RequestPath is { } path)
        {
            context.AddContext("request_path", path);
        }

        if (provider.RequestMethod is { } method)
        {
            context.AddContext("request_method", method);
        }
    }

    private static Activity? StartFeatureActivity<TRequest, TResult>(string featureName, string featureType)
    {
        var activity = FeatureActivitySource.StartActivity($"Feature.{featureType}.{featureName}", ActivityKind.Internal);
        if (activity == null)
        {
            return null;
        }

        activity.SetTag("feature.name", featureName);
        activity.SetTag("feature.type", featureType);
        activity.SetTag("request.type", typeof(TRequest).Name);
        activity.SetTag("result.type", typeof(TResult).Name);
        return activity;
    }

    private static void RecordStageEvent(Activity? activity, string stage, double? durationMs)
    {
        if (activity == null)
        {
            return;
        }

        if (durationMs is null)
        {
            return;
        }

        var tags = new ActivityTagsCollection
        {
            { "stage", stage },
            { "duration_ms", durationMs.Value },
        };

        activity.AddEvent(new ActivityEvent("feature.stage", tags: tags));
    }

    private static void RecordOutcome(Activity? activity, string outcome)
    {
        activity?.SetTag("feature.outcome", outcome);
    }

    private static void RecordException(Activity? activity, Exception ex)
    {
        if (activity == null)
        {
            return;
        }

        activity.AddEvent(new ActivityEvent("exception"));
        activity.SetTag("feature.outcome", OutcomeNames.Exception);
    }

    private static async Task<(TResult Result, double DurationMs)> MeasureAsync<TResult>(Func<Task<TResult>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await action().ConfigureAwait(false);
        stopwatch.Stop();
        return (result, stopwatch.Elapsed.TotalMilliseconds);
    }

    private static class FeatureTypes
    {
        public const string Mutation = "Mutation";
        public const string Query = "Query";
    }

    private static class StageNames
    {
        public const string Validation = "validation";
        public const string Requirements = "requirements";
        public const string Execution = "execution";
        public const string SideEffects = "side_effects";
    }

    private static class OutcomeNames
    {
        public const string Success = "success";
        public const string ValidationFailure = "validation_failure";
        public const string RequirementsFailure = "requirements_failure";
        public const string ExecutionFailure = "execution_failure";
        public const string SideEffectsFailure = "side_effects_failure";
        public const string Exception = "exception";
    }

    private static class MethodNames
    {
        public const string ValidateAsync = "ValidateAsync";
        public const string EnforceAsync = "EnforceAsync";
        public const string ExecuteAsync = "ExecuteAsync";
    }

    private static class ExceptionMessages
    {
        public const string MutatorRequired = "Mutator is required for mutation features.";
        public const string QueryRequired = "Query is required for query features.";
    }
}
