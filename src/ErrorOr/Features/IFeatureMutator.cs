namespace VsaResults;

/// <summary>
/// Executes the core mutation logic.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface IFeatureMutator<TRequest, TResult>
{
    /// <summary>
    /// Executes the mutation.
    /// </summary>
    /// <param name="context">The feature context with request and loaded entities.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result or execution errors.</returns>
    Task<ErrorOr<TResult>> ExecuteAsync(FeatureContext<TRequest> context, CancellationToken ct = default);
}
