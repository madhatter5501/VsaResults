using Microsoft.AspNetCore.Mvc;

namespace VsaResults.Sample.WebApi.Endpoints;

/// <summary>
/// Extension methods for converting ErrorOr errors to Minimal API IResult.
/// </summary>
public static class ResultsExtensions
{
    /// <summary>
    /// Converts a list of errors to an appropriate IResult.
    /// Handles validation errors specially with ValidationProblemDetails.
    /// </summary>
    public static IResult ToResults(this List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An error occurred");
        }

        var firstError = errors[0];
        var statusCode = GetStatusCode(firstError.Type);

        // If all errors are validation errors, return ValidationProblemDetails
        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            var errorDictionary = errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray());

            return Results.ValidationProblem(
                errorDictionary,
                title: "Validation Failed",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Single error - simple problem details
        if (errors.Count == 1)
        {
            return Results.Problem(
                statusCode: statusCode,
                title: GetTitle(firstError.Type),
                detail: firstError.Description,
                extensions: new Dictionary<string, object?>
                {
                    ["errorCode"] = firstError.Code,
                });
        }

        // Multiple errors - include all in extensions
        return Results.Problem(
            statusCode: statusCode,
            title: GetTitle(firstError.Type),
            detail: firstError.Description,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = firstError.Code,
                ["errors"] = errors.Select(e => new { e.Code, e.Description }).ToArray(),
            });
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
