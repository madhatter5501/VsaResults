namespace VsaResults;

/// <summary>
/// Executes a read-only query.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface IFeatureQuery<TRequest, TResult>
{
    /// <summary>
    /// Executes the query.
    /// </summary>
    /// <param name="request">The validated request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result or execution errors.</returns>
    Task<ErrorOr<TResult>> ExecuteAsync(TRequest request, CancellationToken ct = default);
}
