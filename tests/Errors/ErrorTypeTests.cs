using FluentAssertions;
using VsaResults;

namespace Tests;

public class ErrorTypeTests
{
    [Theory]
    [InlineData(ErrorType.Failure, 0)]
    [InlineData(ErrorType.Unexpected, 1)]
    [InlineData(ErrorType.Validation, 2)]
    [InlineData(ErrorType.Conflict, 3)]
    [InlineData(ErrorType.NotFound, 4)]
    [InlineData(ErrorType.Unauthorized, 5)]
    [InlineData(ErrorType.Forbidden, 6)]
    [InlineData(ErrorType.BadRequest, 7)]
    [InlineData(ErrorType.Timeout, 8)]
    [InlineData(ErrorType.Gone, 9)]
    [InlineData(ErrorType.Locked, 10)]
    [InlineData(ErrorType.TooManyRequests, 11)]
    [InlineData(ErrorType.Unavailable, 12)]
    public void ErrorType_ShouldHaveCorrectNumericValue(ErrorType type, int expectedValue)
    {
        // Assert
        ((int)type).Should().Be(expectedValue);
    }

    [Fact]
    public void Error_BadRequest_ShouldCreateBadRequestError()
    {
        // Act
        var error = Error.BadRequest();

        // Assert
        error.Type.Should().Be(ErrorType.BadRequest);
        error.NumericType.Should().Be(7);
        error.Code.Should().Be("General.BadRequest");
    }

    [Fact]
    public void Error_BadRequest_WithCustomValues_ShouldUseCustomValues()
    {
        // Act
        var error = Error.BadRequest(
            code: "Request.Invalid",
            description: "The request was malformed",
            metadata: new Dictionary<string, object> { { "Field", "email" } });

        // Assert
        error.Type.Should().Be(ErrorType.BadRequest);
        error.Code.Should().Be("Request.Invalid");
        error.Description.Should().Be("The request was malformed");
        error.Metadata.Should().ContainKey("Field");
    }

    [Fact]
    public void Error_Timeout_ShouldCreateTimeoutError()
    {
        // Act
        var error = Error.Timeout();

        // Assert
        error.Type.Should().Be(ErrorType.Timeout);
        error.NumericType.Should().Be(8);
        error.Code.Should().Be("General.Timeout");
    }

    [Fact]
    public void Error_Gone_ShouldCreateGoneError()
    {
        // Act
        var error = Error.Gone();

        // Assert
        error.Type.Should().Be(ErrorType.Gone);
        error.NumericType.Should().Be(9);
        error.Code.Should().Be("General.Gone");
    }

    [Fact]
    public void Error_Locked_ShouldCreateLockedError()
    {
        // Act
        var error = Error.Locked();

        // Assert
        error.Type.Should().Be(ErrorType.Locked);
        error.NumericType.Should().Be(10);
        error.Code.Should().Be("General.Locked");
    }

    [Fact]
    public void Error_TooManyRequests_ShouldCreateTooManyRequestsError()
    {
        // Act
        var error = Error.TooManyRequests();

        // Assert
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.NumericType.Should().Be(11);
        error.Code.Should().Be("General.TooManyRequests");
    }

    [Fact]
    public void Error_Unavailable_ShouldCreateUnavailableError()
    {
        // Act
        var error = Error.Unavailable();

        // Assert
        error.Type.Should().Be(ErrorType.Unavailable);
        error.NumericType.Should().Be(12);
        error.Code.Should().Be("General.Unavailable");
    }

    [Fact]
    public void VsaResultFactory_FromError_ShouldCreateErrorOrWithError()
    {
        // Act
        VsaResult<int> errorOr = VsaResultFactory.FromError<int>(Error.BadRequest());

        // Assert
        errorOr.IsError.Should().BeTrue();
        errorOr.FirstError.Type.Should().Be(ErrorType.BadRequest);
    }

    [Fact]
    public void VsaResultFactory_FromErrors_ShouldCreateErrorOrWithMultipleErrors()
    {
        // Act
        VsaResult<int> errorOr = VsaResultFactory.FromErrors<int>(
            Error.BadRequest(),
            Error.Validation());

        // Assert
        errorOr.IsError.Should().BeTrue();
        errorOr.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void AllNewErrorTypes_ShouldBeDistinct()
    {
        // Arrange
        var newErrors = new[]
        {
            Error.BadRequest(),
            Error.Timeout(),
            Error.Gone(),
            Error.Locked(),
            Error.TooManyRequests(),
            Error.Unavailable(),
        };

        // Assert
        var distinctTypes = newErrors.Select(e => e.Type).Distinct().ToList();
        distinctTypes.Should().HaveCount(6);
    }
}
