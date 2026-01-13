using Microsoft.AspNetCore.Http;

namespace VsaResults;

/// <summary>
/// Static helper for converting ErrorOr results to ASP.NET Core IResult.
/// Provides consistent HTTP response generation for Minimal APIs.
/// </summary>
public static class ApiResults
{
    /// <summary>
    /// Returns an OK (200) result with the value, or a problem details result on error.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="result">The ErrorOr result.</param>
    /// <returns>An IResult representing the response.</returns>
    public static IResult Ok<T>(ErrorOr<T> result) =>
        result.Match(
            value => Results.Ok(value),
            errors => ToProblem(errors));

    /// <summary>
    /// Returns a Created (201) result with the value and location, or a problem details result on error.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="result">The ErrorOr result.</param>
    /// <param name="location">The location of the created resource.</param>
    /// <returns>An IResult representing the response.</returns>
    public static IResult Created<T>(ErrorOr<T> result, string location) =>
        result.Match(
            value => Results.Created(location, value),
            errors => ToProblem(errors));

    /// <summary>
    /// Returns a Created (201) result with the value and a location generated from the value.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="result">The ErrorOr result.</param>
    /// <param name="locationSelector">Function to generate the location from the value.</param>
    /// <returns>An IResult representing the response.</returns>
    public static IResult Created<T>(ErrorOr<T> result, Func<T, string> locationSelector) =>
        result.Match(
            value => Results.Created(locationSelector(value), value),
            errors => ToProblem(errors));

    /// <summary>
    /// Returns a NoContent (204) result on success, or a problem details result on error.
    /// </summary>
    /// <param name="result">The ErrorOr result.</param>
    /// <returns>An IResult representing the response.</returns>
    public static IResult NoContent(ErrorOr<Success> result) =>
        result.Match(
            _ => Results.NoContent(),
            errors => ToProblem(errors));

    /// <summary>
    /// Returns a NoContent (204) result on success, or a problem details result on error.
    /// </summary>
    /// <param name="result">The ErrorOr Unit result from side effects.</param>
    /// <returns>An IResult representing the response.</returns>
    public static IResult NoContent(ErrorOr<Unit> result) =>
        result.Match(
            _ => Results.NoContent(),
            errors => ToProblem(errors));

    /// <summary>
    /// Returns an Accepted (202) result with the value, or a problem details result on error.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="result">The ErrorOr result.</param>
    /// <param name="location">Optional location for the status endpoint.</param>
    /// <returns>An IResult representing the response.</returns>
    public static IResult Accepted<T>(ErrorOr<T> result, string? location = null) =>
        result.Match(
            value => Results.Accepted(location, value),
            errors => ToProblem(errors));

    /// <summary>
    /// Converts a list of errors to a Problem Details result.
    /// </summary>
    /// <param name="errors">The errors to convert.</param>
    /// <returns>An IResult representing the problem details.</returns>
    public static IResult ToProblem(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
        }

        var firstError = errors[0];

        // If all errors are validation errors, return a validation problem
        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            return Results.ValidationProblem(
                errors.ToDictionary(e => e.Code, e => new[] { e.Description }),
                title: "Validation Failed",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Problem(
            statusCode: GetStatusCode(firstError.Type),
            title: GetTitle(firstError.Type),
            detail: firstError.Description,
            extensions: GetExtensions(errors));
    }

    private static int GetStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.BadRequest => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Timeout => StatusCodes.Status408RequestTimeout,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Gone => StatusCodes.Status410Gone,
        ErrorType.Locked => StatusCodes.Status423Locked,
        ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
        ErrorType.Failure => StatusCodes.Status500InternalServerError,
        ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError,
    };

    private static string GetTitle(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "Bad Request",
        ErrorType.BadRequest => "Bad Request",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.NotFound => "Not Found",
        ErrorType.Timeout => "Request Timeout",
        ErrorType.Conflict => "Conflict",
        ErrorType.Gone => "Gone",
        ErrorType.Locked => "Locked",
        ErrorType.TooManyRequests => "Too Many Requests",
        ErrorType.Failure => "Internal Server Error",
        ErrorType.Unexpected => "Internal Server Error",
        ErrorType.Unavailable => "Service Unavailable",
        _ => "An error occurred",
    };

    private static Dictionary<string, object?>? GetExtensions(List<Error> errors)
    {
        if (errors.Count <= 1)
        {
            return errors.Count == 1
                ? new Dictionary<string, object?> { ["errorCode"] = errors[0].Code }
                : null;
        }

        return new Dictionary<string, object?>
        {
            ["errorCode"] = errors[0].Code,
            ["errors"] = errors.Select(e => new { e.Code, e.Description }).ToArray(),
        };
    }
}
