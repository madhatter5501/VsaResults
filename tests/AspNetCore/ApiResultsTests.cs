using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace VsaResults.Tests.AspNetCore;

/// <summary>
/// Tests for ApiResults (Minimal API error mapping).
/// These tests ensure parity with ActionResultExtensions for MVC.
/// </summary>
public class ApiResultsTests
{
    [Fact]
    public void Ok_WhenSuccess_ShouldReturnOkResult()
    {
        // Arrange
        ErrorOr<string> result = "test value";

        // Act
        var httpResult = ApiResults.Ok(result);

        // Assert
        httpResult.Should().BeOfType<Ok<string>>();
        var okResult = (Ok<string>)httpResult;
        okResult.Value.Should().Be("test value");
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void Ok_WhenError_ShouldReturnProblemResult()
    {
        // Arrange
        ErrorOr<string> result = Error.NotFound("User.NotFound", "User was not found");

        // Act
        var httpResult = ApiResults.Ok(result);

        // Assert
        httpResult.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)httpResult;
        statusResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Created_WithStaticLocation_WhenSuccess_ShouldReturnCreatedResult()
    {
        // Arrange
        ErrorOr<int> result = 42;

        // Act
        var httpResult = ApiResults.Created(result, "/api/items/42");

        // Assert
        httpResult.Should().BeOfType<Created<int>>();
        var createdResult = (Created<int>)httpResult;
        createdResult.Location.Should().Be("/api/items/42");
        createdResult.Value.Should().Be(42);
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public void Created_WithDynamicLocation_WhenSuccess_ShouldReturnCreatedResult()
    {
        // Arrange
        ErrorOr<int> result = 123;

        // Act
        var httpResult = ApiResults.Created(result, value => $"/api/items/{value}");

        // Assert
        httpResult.Should().BeOfType<Created<int>>();
        var createdResult = (Created<int>)httpResult;
        createdResult.Location.Should().Be("/api/items/123");
    }

    [Fact]
    public void Created_WhenError_ShouldReturnProblemResult()
    {
        // Arrange
        ErrorOr<int> result = Error.Validation("Id.Invalid", "Invalid ID");

        // Act
        var httpResult = ApiResults.Created(result, "/api/items/0");

        // Assert
        httpResult.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)httpResult;
        statusResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void NoContent_WithSuccess_WhenSuccess_ShouldReturnNoContentResult()
    {
        // Arrange
        ErrorOr<Success> result = Result.Success;

        // Act
        var httpResult = ApiResults.NoContent(result);

        // Assert
        httpResult.Should().BeOfType<NoContent>();
    }

    [Fact]
    public void NoContent_WithUnit_WhenSuccess_ShouldReturnNoContentResult()
    {
        // Arrange
        ErrorOr<Unit> result = Unit.Value;

        // Act
        var httpResult = ApiResults.NoContent(result);

        // Assert
        httpResult.Should().BeOfType<NoContent>();
    }

    [Fact]
    public void NoContent_WhenError_ShouldReturnProblemResult()
    {
        // Arrange
        ErrorOr<Success> result = Error.Unauthorized();

        // Act
        var httpResult = ApiResults.NoContent(result);

        // Assert
        httpResult.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)httpResult;
        statusResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void Accepted_WhenSuccess_ShouldReturnAcceptedResult()
    {
        // Arrange
        ErrorOr<string> result = "job-123";

        // Act
        var httpResult = ApiResults.Accepted(result, "/api/jobs/123/status");

        // Assert
        httpResult.Should().BeOfType<Accepted<string>>();
        var acceptedResult = (Accepted<string>)httpResult;
        acceptedResult.Value.Should().Be("job-123");
        acceptedResult.Location.Should().Be("/api/jobs/123/status");
        acceptedResult.StatusCode.Should().Be(StatusCodes.Status202Accepted);
    }

    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.BadRequest, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Timeout, StatusCodes.Status408RequestTimeout)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Gone, StatusCodes.Status410Gone)]
    [InlineData(ErrorType.Locked, StatusCodes.Status423Locked)]
    [InlineData(ErrorType.TooManyRequests, StatusCodes.Status429TooManyRequests)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    [InlineData(ErrorType.Unexpected, StatusCodes.Status500InternalServerError)]
    [InlineData(ErrorType.Unavailable, StatusCodes.Status503ServiceUnavailable)]
    public void ToProblem_ShouldMapErrorTypeToCorrectStatusCode(ErrorType errorType, int expectedStatusCode)
    {
        // Arrange
        var error = Error.Custom((int)errorType, "Test.Error", "Test error description");
        var errors = new List<Error> { error };

        // Act
        var result = ApiResults.ToProblem(errors);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)result;
        statusResult.StatusCode.Should().Be(expectedStatusCode);
    }

    [Theory]
    [InlineData(ErrorType.BadRequest, "Bad Request")]
    [InlineData(ErrorType.Unauthorized, "Unauthorized")]
    [InlineData(ErrorType.Forbidden, "Forbidden")]
    [InlineData(ErrorType.NotFound, "Not Found")]
    [InlineData(ErrorType.Timeout, "Request Timeout")]
    [InlineData(ErrorType.Conflict, "Conflict")]
    [InlineData(ErrorType.Gone, "Gone")]
    [InlineData(ErrorType.Locked, "Locked")]
    [InlineData(ErrorType.TooManyRequests, "Too Many Requests")]
    [InlineData(ErrorType.Failure, "Internal Server Error")]
    [InlineData(ErrorType.Unexpected, "Internal Server Error")]
    [InlineData(ErrorType.Unavailable, "Service Unavailable")]
    public void ToProblem_ShouldMapErrorTypeToCorrectTitle(ErrorType errorType, string expectedTitle)
    {
        // Arrange
        var error = Error.Custom((int)errorType, "Test.Error", "Test error description");
        var errors = new List<Error> { error };

        // Act
        var result = ApiResults.ToProblem(errors);

        // Assert
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.ProblemDetails.Title.Should().Be(expectedTitle);
    }

    [Fact]
    public void ToProblem_WhenAllValidationErrors_ShouldReturnValidationProblem()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Email.Required", "Email is required"),
            Error.Validation("Name.TooShort", "Name must be at least 2 characters"),
        };

        // Act
        var result = ApiResults.ToProblem(errors);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)result;
        statusResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void ToProblem_WhenEmptyErrorList_ShouldReturn500()
    {
        // Arrange
        var errors = new List<Error>();

        // Act
        var result = ApiResults.ToProblem(errors);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)result;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public void ToProblem_WhenCustomErrorType_ShouldReturn500()
    {
        // Arrange
        var customError = Error.Custom(999, "Custom.Error", "A custom error");
        var errors = new List<Error> { customError };

        // Act
        var result = ApiResults.ToProblem(errors);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)result;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public void ToProblem_WhenMultipleErrors_ShouldIncludeErrorsExtension()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.NotFound("User.NotFound", "User was not found"),
            Error.NotFound("Profile.NotFound", "Profile was not found"),
        };

        // Act
        var result = ApiResults.ToProblem(errors);

        // Assert
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.ProblemDetails.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public void ToProblem_WhenSingleError_ShouldIncludeErrorCode()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.NotFound("User.NotFound", "User was not found"),
        };

        // Act
        var result = ApiResults.ToProblem(errors);

        // Assert
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.ProblemDetails.Extensions.Should().ContainKey("errorCode");
        problemResult.ProblemDetails.Extensions["errorCode"].Should().Be("User.NotFound");
    }

    [Fact]
    public void ToProblem_WhenMixedErrorTypes_ShouldUseFirstErrorType()
    {
        // Arrange - First error is Validation (400), second is NotFound (404)
        var errors = new List<Error>
        {
            Error.Validation("Email.Required", "Email is required"),
            Error.NotFound("User.NotFound", "User not found"),
        };

        // Act
        var result = ApiResults.ToProblem(errors);

        // Assert - Should use first error's status code (Validation = 400)
        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)result;
        statusResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}
