namespace VsaResults;

/// <summary>
/// Validates a request before processing.
/// Returns the validated request or validation errors.
/// </summary>
/// <typeparam name="TRequest">The type of the request to validate.</typeparam>
public interface IFeatureValidator<TRequest>
{
    /// <summary>
    /// Validates the request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The validated request or validation errors.</returns>
    Task<ErrorOr<TRequest>> ValidateAsync(TRequest request, CancellationToken ct = default);
}
