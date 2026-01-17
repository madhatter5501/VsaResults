using Microsoft.AspNetCore.Http;

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
            IWideEventEmitter? emitter,
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
            IWideEventEmitter? emitter,
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
            IWideEventEmitter? emitter,
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
            IWideEventEmitter? emitter,
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
            IWideEventEmitter? emitter,
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
            IWideEventEmitter? emitter,
            CancellationToken ct) =>
        {
            var result = await feature.ExecuteAsync(request, emitter, ct);
            return resultMapper(result);
        };
}
