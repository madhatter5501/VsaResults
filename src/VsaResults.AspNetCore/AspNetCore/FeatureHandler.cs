using Microsoft.AspNetCore.Http;
using VsaResults.Binding;
using VsaResults.WideEvents;

namespace VsaResults;

/// <summary>
/// Static handler factory for executing features from Minimal API endpoints.
/// Provides a clean, closure-free way to wire up feature execution.
/// </summary>
/// <example>
/// <code>
/// // Minimal API - simple GET
/// app.MapGet("/users/{id}", FeatureHandler.Query&lt;GetUser.Request, UserDto&gt;(
///     req => ApiResults.Ok(req)));
///
/// // Minimal API - POST with Created response
/// app.MapPost("/users", FeatureHandler.Mutation&lt;CreateUser.Request, UserDto&gt;(
///     (req, result) => ApiResults.Created(result, $"/users/{result.Value.Id}")));
///
/// // With manual binding (emits wide events for binding failures)
/// app.MapGet("/users/{id}", FeatureHandler.QueryOkBound&lt;GetUser.Request, UserDto&gt;());
/// </code>
/// </example>
public static class FeatureHandler
{
    /// <summary>
    /// Creates a delegate handler for a query feature that returns OK on success.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <returns>A delegate that can be used with MapGet/MapPost.</returns>
    public static Delegate QueryOk<TRequest, TResult>()
        where TRequest : notnull =>
        async (
            [AsParameters] TRequest request,
            IQueryFeature<TRequest, TResult> feature,
            IWideEventEmitter emitter,
            CancellationToken ct) =>
        {
            var result = await feature.ExecuteAsync(request, emitter, ct);
            return ApiResults.Ok(result);
        };

    /// <summary>
    /// Creates a delegate handler for a query feature with a custom result mapper.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="resultMapper">Function to map the successful result to an IResult.</param>
    /// <returns>A delegate that can be used with MapGet/MapPost.</returns>
    public static Delegate Query<TRequest, TResult>(Func<VsaResult<TResult>, IResult> resultMapper)
        where TRequest : notnull =>
        async (
            [AsParameters] TRequest request,
            IQueryFeature<TRequest, TResult> feature,
            IWideEventEmitter emitter,
            CancellationToken ct) =>
        {
            var result = await feature.ExecuteAsync(request, emitter, ct);
            return resultMapper(result);
        };

    /// <summary>
    /// Creates a delegate handler for a mutation feature that returns OK on success.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <returns>A delegate that can be used with MapPost/MapPut/MapDelete.</returns>
    public static Delegate MutationOk<TRequest, TResult>()
        where TRequest : notnull =>
        async (
            [AsParameters] TRequest request,
            IMutationFeature<TRequest, TResult> feature,
            IWideEventEmitter emitter,
            CancellationToken ct) =>
        {
            var result = await feature.ExecuteAsync(request, emitter, ct);
            return ApiResults.Ok(result);
        };

    /// <summary>
    /// Creates a delegate handler for a mutation feature that returns Created on success.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="locationSelector">Function to generate the location URI from the result.</param>
    /// <returns>A delegate that can be used with MapPost.</returns>
    public static Delegate MutationCreated<TRequest, TResult>(Func<TResult, string> locationSelector)
        where TRequest : notnull =>
        async (
            [AsParameters] TRequest request,
            IMutationFeature<TRequest, TResult> feature,
            IWideEventEmitter emitter,
            CancellationToken ct) =>
        {
            var result = await feature.ExecuteAsync(request, emitter, ct);
            return ApiResults.Created(result, locationSelector);
        };

    /// <summary>
    /// Creates a delegate handler for a mutation feature that returns NoContent on success.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <returns>A delegate that can be used with MapPut/MapDelete.</returns>
    public static Delegate MutationNoContent<TRequest>()
        where TRequest : notnull =>
        async (
            [AsParameters] TRequest request,
            IMutationFeature<TRequest, Unit> feature,
            IWideEventEmitter emitter,
            CancellationToken ct) =>
        {
            var result = await feature.ExecuteAsync(request, emitter, ct);
            return ApiResults.NoContent(result);
        };

    /// <summary>
    /// Creates a delegate handler for a mutation feature with a custom result mapper.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="resultMapper">Function to map the successful result to an IResult.</param>
    /// <returns>A delegate that can be used with MapPost/MapPut/MapDelete.</returns>
    public static Delegate Mutation<TRequest, TResult>(Func<VsaResult<TResult>, IResult> resultMapper)
        where TRequest : notnull =>
        async (
            [AsParameters] TRequest request,
            IMutationFeature<TRequest, TResult> feature,
            IWideEventEmitter emitter,
            CancellationToken ct) =>
        {
            var result = await feature.ExecuteAsync(request, emitter, ct);
            return resultMapper(result);
        };

    // ==========================================================================
    // Bound handlers - use manual binding for full wide event coverage
    // ==========================================================================

    /// <summary>
    /// Creates a delegate handler for a query feature with manual binding.
    /// Emits wide events for binding failures, providing full observability.
    /// </summary>
    /// <typeparam name="TRequest">The request type (must have parameterless constructor).</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <returns>A delegate that can be used with MapGet/MapPost.</returns>
    public static Delegate QueryOkBound<TRequest, TResult>()
        where TRequest : notnull, new() =>
        async (
            HttpContext httpContext,
            IQueryFeature<TRequest, TResult> feature,
            IWideEventEmitter emitter,
            CancellationToken ct) =>
        {
            var wideEventBuilder = WideEvent.StartFeature(typeof(TRequest).Name, "Query")
                .WithTypes<TRequest, TResult>();

            // Add binding context for debugging
            var bindingContext = RequestBinder.GetBindingContext<TRequest>(httpContext);
            foreach (var (key, value) in bindingContext)
            {
                wideEventBuilder.WithContext(key, value);
            }

            // Manual binding stage
            wideEventBuilder.StartStage("binding", typeof(RequestBinder), "BindAsync");
            var bindingResult = await RequestBinder.BindAsync<TRequest>(httpContext, ct);
            wideEventBuilder.RecordBinding();

            if (bindingResult.IsError)
            {
                var wideEvent = wideEventBuilder.BindingFailure(bindingResult.Errors);
                await emitter.EmitAsync(wideEvent, ct).ConfigureAwait(false);
                return ApiResults.ToProblem(bindingResult.Errors);
            }

            // Extract request context
            wideEventBuilder.WithRequestContext(bindingResult.Value);

            // Continue to feature execution
            var result = await feature.ExecuteAsync(bindingResult.Value, emitter, ct);
            return ApiResults.Ok(result);
        };

    /// <summary>
    /// Creates a delegate handler for a mutation feature with manual binding.
    /// Emits wide events for binding failures, providing full observability.
    /// </summary>
    /// <typeparam name="TRequest">The request type (must have parameterless constructor).</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <returns>A delegate that can be used with MapPost/MapPut/MapDelete.</returns>
    public static Delegate MutationOkBound<TRequest, TResult>()
        where TRequest : notnull, new() =>
        async (
            HttpContext httpContext,
            IMutationFeature<TRequest, TResult> feature,
            IWideEventEmitter emitter,
            CancellationToken ct) =>
        {
            var wideEventBuilder = WideEvent.StartFeature(typeof(TRequest).Name, "Mutation")
                .WithTypes<TRequest, TResult>();

            // Add binding context for debugging
            var bindingContext = RequestBinder.GetBindingContext<TRequest>(httpContext);
            foreach (var (key, value) in bindingContext)
            {
                wideEventBuilder.WithContext(key, value);
            }

            // Manual binding stage
            wideEventBuilder.StartStage("binding", typeof(RequestBinder), "BindAsync");
            var bindingResult = await RequestBinder.BindAsync<TRequest>(httpContext, ct);
            wideEventBuilder.RecordBinding();

            if (bindingResult.IsError)
            {
                var wideEvent = wideEventBuilder.BindingFailure(bindingResult.Errors);
                await emitter.EmitAsync(wideEvent, ct).ConfigureAwait(false);
                return ApiResults.ToProblem(bindingResult.Errors);
            }

            // Extract request context
            wideEventBuilder.WithRequestContext(bindingResult.Value);

            // Continue to feature execution
            var result = await feature.ExecuteAsync(bindingResult.Value, emitter, ct);
            return ApiResults.Ok(result);
        };

    /// <summary>
    /// Creates a delegate handler for a mutation feature with manual binding that returns Created.
    /// Emits wide events for binding failures, providing full observability.
    /// </summary>
    /// <typeparam name="TRequest">The request type (must have parameterless constructor).</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="locationSelector">Function to generate the location URI from the result.</param>
    /// <returns>A delegate that can be used with MapPost.</returns>
    public static Delegate MutationCreatedBound<TRequest, TResult>(Func<TResult, string> locationSelector)
        where TRequest : notnull, new() =>
        async (
            HttpContext httpContext,
            IMutationFeature<TRequest, TResult> feature,
            IWideEventEmitter emitter,
            CancellationToken ct) =>
        {
            var wideEventBuilder = WideEvent.StartFeature(typeof(TRequest).Name, "Mutation")
                .WithTypes<TRequest, TResult>();

            // Add binding context for debugging
            var bindingContext = RequestBinder.GetBindingContext<TRequest>(httpContext);
            foreach (var (key, value) in bindingContext)
            {
                wideEventBuilder.WithContext(key, value);
            }

            // Manual binding stage
            wideEventBuilder.StartStage("binding", typeof(RequestBinder), "BindAsync");
            var bindingResult = await RequestBinder.BindAsync<TRequest>(httpContext, ct);
            wideEventBuilder.RecordBinding();

            if (bindingResult.IsError)
            {
                var wideEvent = wideEventBuilder.BindingFailure(bindingResult.Errors);
                await emitter.EmitAsync(wideEvent, ct).ConfigureAwait(false);
                return ApiResults.ToProblem(bindingResult.Errors);
            }

            // Extract request context
            wideEventBuilder.WithRequestContext(bindingResult.Value);

            // Continue to feature execution
            var result = await feature.ExecuteAsync(bindingResult.Value, emitter, ct);
            return ApiResults.Created(result, locationSelector);
        };

    /// <summary>
    /// Creates a delegate handler for a mutation feature with manual binding that returns NoContent.
    /// Emits wide events for binding failures, providing full observability.
    /// </summary>
    /// <typeparam name="TRequest">The request type (must have parameterless constructor).</typeparam>
    /// <returns>A delegate that can be used with MapPut/MapDelete.</returns>
    public static Delegate MutationNoContentBound<TRequest>()
        where TRequest : notnull, new() =>
        async (
            HttpContext httpContext,
            IMutationFeature<TRequest, Unit> feature,
            IWideEventEmitter emitter,
            CancellationToken ct) =>
        {
            var wideEventBuilder = WideEvent.StartFeature(typeof(TRequest).Name, "Mutation")
                .WithTypes<TRequest, Unit>();

            // Add binding context for debugging
            var bindingContext = RequestBinder.GetBindingContext<TRequest>(httpContext);
            foreach (var (key, value) in bindingContext)
            {
                wideEventBuilder.WithContext(key, value);
            }

            // Manual binding stage
            wideEventBuilder.StartStage("binding", typeof(RequestBinder), "BindAsync");
            var bindingResult = await RequestBinder.BindAsync<TRequest>(httpContext, ct);
            wideEventBuilder.RecordBinding();

            if (bindingResult.IsError)
            {
                var wideEvent = wideEventBuilder.BindingFailure(bindingResult.Errors);
                await emitter.EmitAsync(wideEvent, ct).ConfigureAwait(false);
                return ApiResults.ToProblem(bindingResult.Errors);
            }

            // Extract request context
            wideEventBuilder.WithRequestContext(bindingResult.Value);

            // Continue to feature execution
            var result = await feature.ExecuteAsync(bindingResult.Value, emitter, ct);
            return ApiResults.NoContent(result);
        };
}
