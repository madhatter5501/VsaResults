using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace VsaResults.Tests.AspNetCore;

public class ActionResultExtensionsTests
{
    [Fact]
    public void ToOkResult_WhenSuccess_ShouldReturnOkObjectResult()
    {
        // Arrange
        VsaResult<string> result = "test value";

        // Act
        var actionResult = result.ToOkResult();

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult.Result!;
        okResult.Value.Should().Be("test value");
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void ToOkResult_WhenError_ShouldReturnProblemDetailsResult()
    {
        // Arrange
        VsaResult<string> result = Error.NotFound("User.NotFound", "User was not found");

        // Act
        var actionResult = result.ToOkResult();

        // Assert
        actionResult.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult.Result!;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        objectResult.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task ToOkResult_Async_WhenSuccess_ShouldReturnOkObjectResult()
    {
        // Arrange
        Task<VsaResult<string>> task = Task.FromResult<VsaResult<string>>("async value");

        // Act
        var actionResult = await task.ToOkResult();

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult.Result!;
        okResult.Value.Should().Be("async value");
    }

    [Fact]
    public void ToCreatedResult_WithStaticLocation_WhenSuccess_ShouldReturnCreatedResult()
    {
        // Arrange
        VsaResult<int> result = 42;

        // Act
        var actionResult = result.ToCreatedResult("/api/items/42");

        // Assert
        actionResult.Result.Should().BeOfType<CreatedResult>();
        var createdResult = (CreatedResult)actionResult.Result!;
        createdResult.Location.Should().Be("/api/items/42");
        createdResult.Value.Should().Be(42);
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public void ToCreatedResult_WithDynamicLocation_WhenSuccess_ShouldReturnCreatedResult()
    {
        // Arrange
        VsaResult<int> result = 123;

        // Act
        var actionResult = result.ToCreatedResult(value => $"/api/items/{value}");

        // Assert
        actionResult.Result.Should().BeOfType<CreatedResult>();
        var createdResult = (CreatedResult)actionResult.Result!;
        createdResult.Location.Should().Be("/api/items/123");
    }

    [Fact]
    public async Task ToCreatedResult_Async_WithDynamicLocation_WhenSuccess_ShouldReturnCreatedResult()
    {
        // Arrange
        Task<VsaResult<int>> task = Task.FromResult<VsaResult<int>>(456);

        // Act
        var actionResult = await task.ToCreatedResult(value => $"/api/items/{value}");

        // Assert
        actionResult.Result.Should().BeOfType<CreatedResult>();
        var createdResult = (CreatedResult)actionResult.Result!;
        createdResult.Location.Should().Be("/api/items/456");
    }

    [Fact]
    public void ToNoContentResult_WithSuccess_WhenSuccess_ShouldReturnNoContentResult()
    {
        // Arrange
        VsaResult<Success> result = Result.Success;

        // Act
        var actionResult = result.ToNoContentResult();

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void ToNoContentResult_WithUnit_WhenSuccess_ShouldReturnNoContentResult()
    {
        // Arrange
        VsaResult<Unit> result = Unit.Value;

        // Act
        var actionResult = result.ToNoContentResult();

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ToNoContentResult_Async_WhenSuccess_ShouldReturnNoContentResult()
    {
        // Arrange
        Task<VsaResult<Unit>> task = Task.FromResult<VsaResult<Unit>>(Unit.Value);

        // Act
        var actionResult = await task.ToNoContentResult();

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void ToNoContentResult_WhenError_ShouldReturnProblemDetailsResult()
    {
        // Arrange
        VsaResult<Success> result = Error.Unauthorized();

        // Act
        var actionResult = result.ToNoContentResult();

        // Assert
        actionResult.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
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
    public void ToProblemDetailsResult_ShouldMapErrorTypeToCorrectStatusCode(ErrorType errorType, int expectedStatusCode)
    {
        // Arrange
        var error = Error.Custom((int)errorType, "Test.Error", "Test error description");
        var errors = new List<Error> { error };

        // Act
        var result = errors.ToProblemDetailsResult();

        // Assert
        result.StatusCode.Should().Be(expectedStatusCode);
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
    public void ToProblemDetailsResult_ShouldMapErrorTypeToCorrectTitle(ErrorType errorType, string expectedTitle)
    {
        // Arrange
        var error = Error.Custom((int)errorType, "Test.Error", "Test error description");
        var errors = new List<Error> { error };

        // Act
        var result = errors.ToProblemDetailsResult();

        // Assert
        var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be(expectedTitle);
    }

    [Fact]
    public void ToProblemDetailsResult_WhenValidationError_ShouldReturnValidationProblemDetailsWithDefaultTitle()
    {
        // Arrange
        var error = Error.Validation("Test.Error", "Test error description");
        var errors = new List<Error> { error };

        // Act
        var result = errors.ToProblemDetailsResult();

        // Assert
        var validationProblem = result.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        validationProblem.Title.Should().Be("Validation Failed");
    }

    [Fact]
    public void ToProblemDetailsResult_WhenAllValidationErrors_ShouldReturnValidationProblemDetails()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Email.Required", "Email is required"),
            Error.Validation("Name.TooShort", "Name must be at least 2 characters"),
        };

        // Act
        var result = errors.ToProblemDetailsResult();

        // Assert
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.Value.Should().BeOfType<ValidationProblemDetails>();
        var validationProblem = (ValidationProblemDetails)result.Value!;
        validationProblem.Title.Should().Be("Validation Failed");
        validationProblem.Errors.Should().ContainKey("Email.Required");
        validationProblem.Errors.Should().ContainKey("Name.TooShort");
    }

    [Fact]
    public void ToProblemDetailsResult_WhenDuplicateValidationErrorCodes_ShouldGroupDescriptions()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Email", "Email is required"),
            Error.Validation("Email", "Email format is invalid"),
            Error.Validation("Email", "Email domain is not allowed"),
        };

        // Act
        var result = errors.ToProblemDetailsResult();

        // Assert
        result.Value.Should().BeOfType<ValidationProblemDetails>();
        var validationProblem = (ValidationProblemDetails)result.Value!;
        validationProblem.Errors.Should().ContainKey("Email");
        validationProblem.Errors["Email"].Should().HaveCount(3);
        validationProblem.Errors["Email"].Should().Contain("Email is required");
        validationProblem.Errors["Email"].Should().Contain("Email format is invalid");
        validationProblem.Errors["Email"].Should().Contain("Email domain is not allowed");
    }

    [Fact]
    public void ToProblemDetailsResult_WhenCustomTitle_ShouldUseCustomTitle()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Field.Invalid", "Field is invalid"),
        };

        // Act
        var result = errors.ToProblemDetailsResult("Custom Validation Title");

        // Assert
        var validationProblem = (ValidationProblemDetails)result.Value!;
        validationProblem.Title.Should().Be("Custom Validation Title");
    }

    [Fact]
    public void ToProblemDetailsResult_WhenMixedErrorTypes_ShouldReturnRegularProblemDetails()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Email.Required", "Email is required"),
            Error.NotFound("User.NotFound", "User not found"),
        };

        // Act
        var result = errors.ToProblemDetailsResult();

        // Assert
        result.Value.Should().BeOfType<ProblemDetails>();
        var problemDetails = (ProblemDetails)result.Value!;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void ToProblemDetailsResult_WhenMultipleNonValidationErrors_ShouldIncludeAllInExtensions()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.NotFound("User.NotFound", "User was not found"),
            Error.NotFound("Profile.NotFound", "Profile was not found"),
        };

        // Act
        var result = errors.ToProblemDetailsResult();

        // Assert
        result.Value.Should().BeOfType<ProblemDetails>();
        var problemDetails = (ProblemDetails)result.Value!;
        problemDetails.Extensions.Should().ContainKey("errors");

        var errorsExtension = problemDetails.Extensions["errors"] as IEnumerable<object>;
        errorsExtension.Should().NotBeNull();
        errorsExtension.Should().HaveCount(2);
    }

    [Fact]
    public void ToProblemDetailsResult_WhenSingleError_ShouldNotIncludeErrorsExtension()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.NotFound("User.NotFound", "User was not found"),
        };

        // Act
        var result = errors.ToProblemDetailsResult();

        // Assert
        var problemDetails = (ProblemDetails)result.Value!;
        problemDetails.Extensions.Should().ContainKey("errorCode");
        problemDetails.Extensions.Should().NotContainKey("errors");
        problemDetails.Extensions["errorCode"].Should().Be("User.NotFound");
    }

    [Fact]
    public void ToProblemDetailsResult_WhenEmptyErrorList_ShouldReturn500()
    {
        // Arrange
        var errors = new List<Error>();

        // Act
        var result = errors.ToProblemDetailsResult();

        // Assert
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var problemDetails = (ProblemDetails)result.Value!;
        problemDetails.Title.Should().Be("An error occurred");
    }

    [Fact]
    public void ToProblemDetailsResult_WhenCustomErrorType_ShouldReturn500()
    {
        // Arrange
        var customError = Error.Custom(999, "Custom.Error", "A custom error");
        var errors = new List<Error> { customError };

        // Act
        var result = errors.ToProblemDetailsResult();

        // Assert
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
