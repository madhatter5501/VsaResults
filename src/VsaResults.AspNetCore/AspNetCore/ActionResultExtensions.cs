using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace VsaResults;

/// <summary>
/// Extension methods for converting ErrorOr results to ActionResult for MVC Controllers.
/// </summary>
public static class ActionResultExtensions
{
    /// <summary>
    /// Converts an ErrorOr result to an OK (200) ActionResult or a problem details result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="result">The ErrorOr result.</param>
    /// <returns>An ActionResult representing the response.</returns>
    public static ActionResult<T> ToOkResult<T>(this VsaResult<T> result) =>
        result.Match<ActionResult<T>>(
            value => new OkObjectResult(value),
            errors => ToProblemDetailsResult(errors));

    /// <summary>
    /// Converts an async ErrorOr result to an OK (200) ActionResult or a problem details result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="task">The async ErrorOr result.</param>
    /// <returns>An ActionResult representing the response.</returns>
    public static Task<ActionResult<T>> ToOkResult<T>(this Task<VsaResult<T>> task) =>
        task.ThenSync(result => result.ToOkResult());

    /// <summary>
    /// Converts an ErrorOr result to a Created (201) ActionResult or a problem details result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="result">The ErrorOr result.</param>
    /// <param name="location">The location of the created resource.</param>
    /// <returns>An ActionResult representing the response.</returns>
    public static ActionResult<T> ToCreatedResult<T>(this VsaResult<T> result, string location) =>
        result.Match<ActionResult<T>>(
            value => new CreatedResult(location, value),
            errors => ToProblemDetailsResult(errors));

    /// <summary>
    /// Converts an ErrorOr result to a Created (201) ActionResult with a dynamic location.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="result">The ErrorOr result.</param>
    /// <param name="locationSelector">Function to generate the location from the value.</param>
    /// <returns>An ActionResult representing the response.</returns>
    public static ActionResult<T> ToCreatedResult<T>(this VsaResult<T> result, Func<T, string> locationSelector) =>
        result.Match<ActionResult<T>>(
            value => new CreatedResult(locationSelector(value), value),
            errors => ToProblemDetailsResult(errors));

    /// <summary>
    /// Converts an async ErrorOr result to a Created (201) ActionResult or a problem details result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="task">The async ErrorOr result.</param>
    /// <param name="locationSelector">Function to generate the location from the value.</param>
    /// <returns>An ActionResult representing the response.</returns>
    public static Task<ActionResult<T>> ToCreatedResult<T>(this Task<VsaResult<T>> task, Func<T, string> locationSelector) =>
        task.ThenSync(result => result.ToCreatedResult(locationSelector));

    /// <summary>
    /// Converts an ErrorOr result to a NoContent (204) ActionResult or a problem details result.
    /// </summary>
    /// <param name="result">The ErrorOr result.</param>
    /// <returns>An IActionResult representing the response.</returns>
    public static IActionResult ToNoContentResult(this VsaResult<Success> result) =>
        result.Match<IActionResult>(
            _ => new NoContentResult(),
            errors => ToProblemDetailsResult(errors));

    /// <summary>
    /// Converts an ErrorOr Unit result to a NoContent (204) ActionResult or a problem details result.
    /// </summary>
    /// <param name="result">The ErrorOr Unit result.</param>
    /// <returns>An IActionResult representing the response.</returns>
    public static IActionResult ToNoContentResult(this VsaResult<Unit> result) =>
        result.Match<IActionResult>(
            _ => new NoContentResult(),
            errors => ToProblemDetailsResult(errors));

    /// <summary>
    /// Converts an async ErrorOr result to a NoContent (204) ActionResult or a problem details result.
    /// </summary>
    /// <param name="task">The async ErrorOr result.</param>
    /// <returns>An IActionResult representing the response.</returns>
    public static Task<IActionResult> ToNoContentResult(this Task<VsaResult<Unit>> task) =>
        task.ThenSync(result => result.ToNoContentResult());

    /// <summary>
    /// Converts a list of errors to a ProblemDetails ActionResult.
    /// </summary>
    /// <param name="errors">The errors to convert.</param>
    /// <param name="title">Optional custom title.</param>
    /// <returns>An ObjectResult containing problem details.</returns>
    public static ObjectResult ToProblemDetailsResult(this List<Error> errors, string? title = null)
    {
        if (errors.Count == 0)
        {
            return new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            };
        }

        var firstError = errors[0];
        var statusCode = GetStatusCode(firstError.Type);

        // If all errors are validation errors, return a ValidationProblemDetails
        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            // Group by error code to handle duplicate codes (multiple validation errors for the same field)
            var errorDictionary = errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray());

            var validationProblem = new ValidationProblemDetails(errorDictionary)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = title ?? "Validation Failed",
            };

            return new ObjectResult(validationProblem)
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title ?? GetTitle(firstError.Type),
            Detail = firstError.Description,
        };

        problemDetails.Extensions["errorCode"] = firstError.Code;

        if (errors.Count > 1)
        {
            problemDetails.Extensions["errors"] = errors.Select(e => new { e.Code, e.Description }).ToArray();
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
        };
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
}
