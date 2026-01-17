using VsaResults.WideEvents;

namespace VsaResults;

/// <summary>
/// Extension methods for executing features with automatic wide event emission.
/// </summary>
public static class FeatureExecutionExtensions
{
    /// <summary>
    /// Executes a mutation feature through the full pipeline:
    /// Validate → Enforce Requirements → Execute Mutation → Run Side Effects → Emit Wide Event.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="feature">The feature to execute.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="emitter">Optional wide event emitter. If null, no event is emitted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result or errors from execution.</returns>
    public static async Task<ErrorOr<TResult>> ExecuteAsync<TRequest, TResult>(
        this IMutationFeature<TRequest, TResult> feature,
        TRequest request,
        IWideEventEmitter? emitter = null,
        CancellationToken ct = default)
    {
        var featureName = feature.GetType().DeclaringType?.Name ?? feature.GetType().Name;
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
            var validated = await validator.ValidateAsync(request, ct).ConfigureAwait(false);
            wideEvent.RecordValidation();

            if (validated.IsError)
            {
                wideEvent.WithErrorOrContext(validated.Context);
                EmitWideEvent(emitter, wideEvent.WithFeatureContext(context).ValidationFailure(validated.Errors));
                return new ErrorOr<TResult>(validated.Errors, validated._context);
            }

            // Requirements stage
            var requirements = feature.Requirements ?? NoOpRequirements<TRequest>.Instance;
            wideEvent.StartStage(StageNames.Requirements, requirements.GetType(), MethodNames.EnforceAsync);
            var enforced = await requirements.EnforceAsync(validated.Value, ct).ConfigureAwait(false);
            wideEvent.RecordRequirements();

            if (enforced.IsError)
            {
                wideEvent.WithErrorOrContext(enforced.Context);
                EmitWideEvent(emitter, wideEvent.WithFeatureContext(context).RequirementsFailure(enforced.Errors));
                return new ErrorOr<TResult>(enforced.Errors, enforced._context);
            }

            context = enforced.Value;

            // Execution stage
            var mutator = feature.Mutator ?? throw new InvalidOperationException(ExceptionMessages.MutatorRequired);
            wideEvent.StartStage(StageNames.Execution, mutator.GetType(), MethodNames.ExecuteAsync);
            var result = await mutator.ExecuteAsync(context, ct).ConfigureAwait(false);
            wideEvent.RecordExecution();

            if (result.IsError)
            {
                wideEvent.WithErrorOrContext(result.Context);
                EmitWideEvent(emitter, wideEvent.WithFeatureContext(context).ExecutionFailure(result.Errors));
                return result;
            }

            // Side effects stage
            var sideEffects = feature.SideEffects ?? NoOpSideEffects<TRequest>.Instance;
            wideEvent.StartStage(StageNames.SideEffects, sideEffects.GetType(), MethodNames.ExecuteAsync);
            var effects = await sideEffects.ExecuteAsync(context, ct).ConfigureAwait(false);
            wideEvent.RecordSideEffects();

            if (effects.IsError)
            {
                wideEvent.WithErrorOrContext(effects.Context);
                EmitWideEvent(emitter, wideEvent.WithFeatureContext(context).SideEffectsFailure(effects.Errors));
                return new ErrorOr<TResult>(effects.Errors, effects._context);
            }

            wideEvent.WithErrorOrContext(result.Context);
            EmitWideEvent(emitter, wideEvent.WithFeatureContext(context).Success());
            return result;
        }
        catch (Exception ex)
        {
            EmitWideEvent(emitter, wideEvent.WithFeatureContext(context).Exception(ex));
            throw;
        }
    }

    /// <summary>
    /// Executes a query feature through the pipeline:
    /// Validate → Execute Query → Emit Wide Event.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="feature">The feature to execute.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="emitter">Optional wide event emitter. If null, no event is emitted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result or errors from execution.</returns>
    public static async Task<ErrorOr<TResult>> ExecuteAsync<TRequest, TResult>(
        this IQueryFeature<TRequest, TResult> feature,
        TRequest request,
        IWideEventEmitter? emitter = null,
        CancellationToken ct = default)
    {
        var featureName = feature.GetType().DeclaringType?.Name ?? feature.GetType().Name;
        var wideEvent = FeatureWideEvent.Start(featureName, FeatureTypes.Query)
            .WithTypes<TRequest, TResult>()
            .WithRequestContext(request)
            .WithPipelineStages(
                feature.Validator?.GetType(),
                requirementsType: null,
                feature.Query?.GetType(),
                sideEffectsType: null,
                isMutation: false);

        try
        {
            // Validation stage
            var validator = feature.Validator ?? NoOpValidator<TRequest>.Instance;
            wideEvent.StartStage(StageNames.Validation, validator.GetType(), MethodNames.ValidateAsync);
            var validated = await validator.ValidateAsync(request, ct).ConfigureAwait(false);
            wideEvent.RecordValidation();

            if (validated.IsError)
            {
                wideEvent.WithErrorOrContext(validated.Context);
                EmitWideEvent(emitter, wideEvent.ValidationFailure(validated.Errors));
                return new ErrorOr<TResult>(validated.Errors, validated._context);
            }

            // Execution stage
            var query = feature.Query ?? throw new InvalidOperationException(ExceptionMessages.QueryRequired);
            wideEvent.StartStage(StageNames.Execution, query.GetType(), MethodNames.ExecuteAsync);
            var result = await query.ExecuteAsync(validated.Value, ct).ConfigureAwait(false);
            wideEvent.RecordExecution();

            if (result.IsError)
            {
                wideEvent.WithErrorOrContext(result.Context);
                EmitWideEvent(emitter, wideEvent.ExecutionFailure(result.Errors));
                return result;
            }

            wideEvent.WithErrorOrContext(result.Context);
            EmitWideEvent(emitter, wideEvent.Success());
            return result;
        }
        catch (Exception ex)
        {
            EmitWideEvent(emitter, wideEvent.Exception(ex));
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
    public static async Task<ErrorOr<TResult>> ExecuteAsync<TRequest, TResult>(
        this IMutationFeature<TRequest, TResult> feature,
        TRequest request,
        IUnifiedWideEventEmitter emitter,
        CancellationToken ct = default)
    {
        var featureName = feature.GetType().DeclaringType?.Name ?? feature.GetType().Name;
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
            var validated = await validator.ValidateAsync(request, ct).ConfigureAwait(false);
            builder.RecordValidation();

            if (validated.IsError)
            {
                builder.WithErrorOrContext(validated.Context);
                emitter.Emit(builder.WithFeatureContext(context).ValidationFailure(validated.Errors));
                return new ErrorOr<TResult>(validated.Errors, validated._context);
            }

            // Requirements stage
            var requirements = feature.Requirements ?? NoOpRequirements<TRequest>.Instance;
            builder.StartStage(StageNames.Requirements, requirements.GetType(), MethodNames.EnforceAsync);
            var enforced = await requirements.EnforceAsync(validated.Value, ct).ConfigureAwait(false);
            builder.RecordRequirements();

            if (enforced.IsError)
            {
                builder.WithErrorOrContext(enforced.Context);
                emitter.Emit(builder.WithFeatureContext(context).RequirementsFailure(enforced.Errors));
                return new ErrorOr<TResult>(enforced.Errors, enforced._context);
            }

            context = enforced.Value;

            // Execution stage
            var mutator = feature.Mutator ?? throw new InvalidOperationException(ExceptionMessages.MutatorRequired);
            builder.StartStage(StageNames.Execution, mutator.GetType(), MethodNames.ExecuteAsync);
            var result = await mutator.ExecuteAsync(context, ct).ConfigureAwait(false);
            builder.RecordExecution();

            if (result.IsError)
            {
                builder.WithErrorOrContext(result.Context);
                emitter.Emit(builder.WithFeatureContext(context).ExecutionFailure(result.Errors));
                return result;
            }

            // Side effects stage
            var sideEffects = feature.SideEffects ?? NoOpSideEffects<TRequest>.Instance;
            builder.StartStage(StageNames.SideEffects, sideEffects.GetType(), MethodNames.ExecuteAsync);
            var effects = await sideEffects.ExecuteAsync(context, ct).ConfigureAwait(false);
            builder.RecordSideEffects();

            if (effects.IsError)
            {
                builder.WithErrorOrContext(effects.Context);
                emitter.Emit(builder.WithFeatureContext(context).SideEffectsFailure(effects.Errors));
                return new ErrorOr<TResult>(effects.Errors, effects._context);
            }

            builder.WithErrorOrContext(result.Context);
            emitter.Emit(builder.WithFeatureContext(context).Success());
            return result;
        }
        catch (Exception ex)
        {
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
    public static async Task<ErrorOr<TResult>> ExecuteAsync<TRequest, TResult>(
        this IQueryFeature<TRequest, TResult> feature,
        TRequest request,
        IUnifiedWideEventEmitter emitter,
        CancellationToken ct = default)
    {
        var featureName = feature.GetType().DeclaringType?.Name ?? feature.GetType().Name;
        var builder = WideEvent.StartFeature(featureName, FeatureTypes.Query)
            .WithTypes<TRequest, TResult>()
            .WithRequestContext(request)
            .WithPipelineStages(
                feature.Validator?.GetType(),
                requirementsType: null,
                feature.Query?.GetType(),
                sideEffectsType: null,
                isMutation: false);

        try
        {
            // Validation stage
            var validator = feature.Validator ?? NoOpValidator<TRequest>.Instance;
            builder.StartStage(StageNames.Validation, validator.GetType(), MethodNames.ValidateAsync);
            var validated = await validator.ValidateAsync(request, ct).ConfigureAwait(false);
            builder.RecordValidation();

            if (validated.IsError)
            {
                builder.WithErrorOrContext(validated.Context);
                emitter.Emit(builder.ValidationFailure(validated.Errors));
                return new ErrorOr<TResult>(validated.Errors, validated._context);
            }

            // Execution stage
            var query = feature.Query ?? throw new InvalidOperationException(ExceptionMessages.QueryRequired);
            builder.StartStage(StageNames.Execution, query.GetType(), MethodNames.ExecuteAsync);
            var result = await query.ExecuteAsync(validated.Value, ct).ConfigureAwait(false);
            builder.RecordExecution();

            if (result.IsError)
            {
                builder.WithErrorOrContext(result.Context);
                emitter.Emit(builder.ExecutionFailure(result.Errors));
                return result;
            }

            builder.WithErrorOrContext(result.Context);
            emitter.Emit(builder.Success());
            return result;
        }
        catch (Exception ex)
        {
            emitter.Emit(builder.Exception(ex));
            throw;
        }
    }

    private static void EmitWideEvent(IWideEventEmitter? emitter, FeatureWideEvent wideEvent)
    {
        emitter?.Emit(wideEvent);
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
