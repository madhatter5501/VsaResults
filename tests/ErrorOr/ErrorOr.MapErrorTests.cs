using FluentAssertions;
using VsaResults;

namespace Tests;

public class MapErrorTests
{
    [Fact]
    public void MapError_WhenError_ShouldTransformEachError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new List<Error>
        {
            Error.Validation(code: "Field.A"),
            Error.Validation(code: "Field.B"),
        };

        // Act
        var result = errorOrInt.MapError(error =>
            Error.Validation(code: $"Prefixed.{error.Code}", description: error.Description));

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors[0].Code.Should().Be("Prefixed.Field.A");
        result.Errors[1].Code.Should().Be("Prefixed.Field.B");
    }

    [Fact]
    public void MapError_WhenSuccess_ShouldReturnOriginal()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 42;

        // Act
        var result = errorOrInt.MapError(error =>
            Error.Validation(code: $"Prefixed.{error.Code}"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void MapErrors_WhenError_ShouldTransformEntireList()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new List<Error>
        {
            Error.Validation(code: "Field.A"),
            Error.Validation(code: "Field.B"),
            Error.Validation(code: "Field.C"),
        };

        // Act
        var result = errorOrInt.MapErrors(errors =>
            errors.Take(1).Select(e => Error.Conflict(code: e.Code)).ToList());

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Type.Should().Be(ErrorType.Conflict);
        result.Errors[0].Code.Should().Be("Field.A");
    }

    [Fact]
    public void MapErrors_WhenSuccess_ShouldReturnOriginal()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 42;

        // Act
        var result = errorOrInt.MapErrors(errors => new List<Error> { Error.Unexpected() });

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task MapErrorAsync_WhenError_ShouldTransformEachErrorAsync()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Error.NotFound(code: "User.NotFound");

        // Act
        var result = await errorOrInt.MapErrorAsync(async error =>
        {
            await Task.Delay(1);
            return Error.Validation(code: $"Async.{error.Code}");
        });

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Async.User.NotFound");
    }

    [Fact]
    public async Task MapErrorAsync_WhenSuccess_ShouldReturnOriginal()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 42;

        // Act
        var result = await errorOrInt.MapErrorAsync(async error =>
        {
            await Task.Delay(1);
            return Error.Validation(code: "Should.Not.Happen");
        });

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task MapError_OnTaskErrorOr_ShouldWorkCorrectly()
    {
        // Arrange
        Task<ErrorOr<int>> taskErrorOr = Task.FromResult<ErrorOr<int>>(Error.NotFound(code: "Test"));

        // Act
        var result = await taskErrorOr.MapError(error =>
            Error.Validation(code: $"Mapped.{error.Code}"));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Mapped.Test");
    }

    [Fact]
    public void MapError_CanBeUsedToAddMetadata()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Error.Validation(code: "Field.Required");

        // Act
        var result = errorOrInt.MapError(error =>
            Error.Validation(
                code: error.Code,
                description: error.Description,
                metadata: new Dictionary<string, object> { { "Timestamp", DateTime.UtcNow } }));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Metadata.Should().ContainKey("Timestamp");
    }
}
