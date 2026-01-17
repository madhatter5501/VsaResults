using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace VsaResults;

/// <summary>
/// Extension methods for mapping feature endpoints with minimal boilerplate.
/// </summary>
/// <example>
/// <code>
/// // Ultra-minimal endpoint registration
/// app.MapGetFeature&lt;GetUser.Request, UserDto&gt;("/users/{id}");
/// app.MapPostFeature&lt;CreateUser.Request, UserDto&gt;("/users", result => $"/users/{result.Id}");
/// app.MapPutFeature&lt;UpdateUser.Request, UserDto&gt;("/users/{id}");
/// app.MapDeleteFeature&lt;DeleteUser.Request&gt;("/users/{id}");
/// </code>
/// </example>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a GET endpoint for a query feature. Returns OK (200) on success.
    /// </summary>
    /// <typeparam name="TRequest">The request type (bound from route/query parameters via [AsParameters]).</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    public static RouteHandlerBuilder MapGetFeature<TRequest, TResult>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : notnull =>
        endpoints.MapGet(pattern, FeatureHandler.QueryOk<TRequest, TResult>());

    /// <summary>
    /// Maps a GET endpoint for a query feature with a custom result mapper.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="resultMapper">Function to map the result to an IResult.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    public static RouteHandlerBuilder MapGetFeature<TRequest, TResult>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<VsaResult<TResult>, IResult> resultMapper)
        where TRequest : notnull =>
        endpoints.MapGet(pattern, FeatureHandler.Query<TRequest, TResult>(resultMapper));

    /// <summary>
    /// Maps a POST endpoint for a mutation feature. Returns Created (201) on success.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="locationSelector">Function to generate the location URI from the result.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    public static RouteHandlerBuilder MapPostFeature<TRequest, TResult>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<TResult, string> locationSelector)
        where TRequest : notnull =>
        endpoints.MapPost(pattern, FeatureHandler.MutationCreated<TRequest, TResult>(locationSelector));

    /// <summary>
    /// Maps a POST endpoint for a mutation feature. Returns OK (200) on success.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    public static RouteHandlerBuilder MapPostFeatureOk<TRequest, TResult>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : notnull =>
        endpoints.MapPost(pattern, FeatureHandler.MutationOk<TRequest, TResult>());

    /// <summary>
    /// Maps a PUT endpoint for a mutation feature. Returns OK (200) on success.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    public static RouteHandlerBuilder MapPutFeature<TRequest, TResult>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : notnull =>
        endpoints.MapPut(pattern, FeatureHandler.MutationOk<TRequest, TResult>());

    /// <summary>
    /// Maps a PUT endpoint for a mutation feature with a custom result mapper.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="resultMapper">Function to map the result to an IResult.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    public static RouteHandlerBuilder MapPutFeature<TRequest, TResult>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<VsaResult<TResult>, IResult> resultMapper)
        where TRequest : notnull =>
        endpoints.MapPut(pattern, FeatureHandler.Mutation<TRequest, TResult>(resultMapper));

    /// <summary>
    /// Maps a DELETE endpoint for a mutation feature. Returns NoContent (204) on success.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    public static RouteHandlerBuilder MapDeleteFeature<TRequest>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : notnull =>
        endpoints.MapDelete(pattern, FeatureHandler.MutationNoContent<TRequest>());

    /// <summary>
    /// Maps a DELETE endpoint for a mutation feature that returns a result. Returns OK (200) on success.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    public static RouteHandlerBuilder MapDeleteFeature<TRequest, TResult>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : notnull =>
        endpoints.MapDelete(pattern, FeatureHandler.MutationOk<TRequest, TResult>());

    /// <summary>
    /// Maps a PATCH endpoint for a mutation feature. Returns OK (200) on success.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    public static RouteHandlerBuilder MapPatchFeature<TRequest, TResult>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : notnull =>
        endpoints.MapPatch(pattern, FeatureHandler.MutationOk<TRequest, TResult>());
}
