namespace VsaResults;

/// <summary>
/// Enforces business requirements and authorization.
/// Loads required entities and creates the feature context.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
public interface IFeatureRequirements<TRequest>
{
    /// <summary>
    /// Enforces requirements and creates the feature context with loaded entities.
    /// </summary>
    /// <param name="request">The validated request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The feature context or requirement failures.</returns>
    Task<ErrorOr<FeatureContext<TRequest>>> EnforceAsync(TRequest request, CancellationToken ct = default);
}
