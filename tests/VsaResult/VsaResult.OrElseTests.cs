using FluentAssertions;
using VsaResults;

namespace Tests;

public class OrElseTests
{
    [Fact]
    public void OrElse_WhenSuccess_ShouldReturnOriginal()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;

        // Act
        var result = errorOrInt.OrElse(errors => 0);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void OrElse_WhenError_ShouldReturnFallbackValue()
    {
        // Arrange
        VsaResult<int> errorOrInt = Error.NotFound();

        // Act
        var result = errorOrInt.OrElse(errors => 99);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(99);
    }

    [Fact]
    public void OrElse_WhenError_FallbackCanAlsoFail()
    {
        // Arrange
        VsaResult<int> errorOrInt = Error.NotFound(code: "Primary.Error");

        // Act
        var result = errorOrInt.OrElse(errors => Error.Validation(code: "Fallback.Error"));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Fallback.Error");
    }

    [Fact]
    public void OrElse_WithErrorOrFallback_WhenSuccess_ShouldReturnOriginal()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;
        VsaResult<int> fallback = 99;

        // Act
        var result = errorOrInt.OrElse(fallback);

        // Assert
        result.Value.Should().Be(42);
    }

    [Fact]
    public void OrElse_WithErrorOrFallback_WhenError_ShouldReturnFallback()
    {
        // Arrange
        VsaResult<int> errorOrInt = Error.NotFound();
        VsaResult<int> fallback = 99;

        // Act
        var result = errorOrInt.OrElse(fallback);

        // Assert
        result.Value.Should().Be(99);
    }

    [Fact]
    public void OrElseFirst_WhenError_ShouldReceiveFirstError()
    {
        // Arrange
        VsaResult<int> errorOrInt = new List<Error>
        {
            Error.NotFound(code: "First"),
            Error.Validation(code: "Second"),
        };
        Error? receivedError = null;

        // Act
        var result = errorOrInt.OrElseFirst(error =>
        {
            receivedError = error;
            return 42;
        });

        // Assert
        receivedError.Should().NotBeNull();
        receivedError!.Value.Code.Should().Be("First");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void OrElse_CanBeChained()
    {
        // Arrange
        VsaResult<int> primary = Error.NotFound(code: "Primary");
        VsaResult<int> secondary = Error.NotFound(code: "Secondary");
        VsaResult<int> tertiary = 42;

        // Act
        var result = primary
            .OrElse(_ => secondary)
            .OrElse(_ => tertiary);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task OrElseAsync_WhenError_ShouldExecuteAsyncFallback()
    {
        // Arrange
        VsaResult<int> errorOrInt = Error.NotFound();

        // Act
        var result = await errorOrInt.OrElseAsync(async errors =>
        {
            await Task.Delay(1);
            return (VsaResult<int>)42;
        });

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task OrElseAsync_WhenSuccess_ShouldNotExecuteAsyncFallback()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;
        var fallbackExecuted = false;

        // Act
        var result = await errorOrInt.OrElseAsync(async errors =>
        {
            fallbackExecuted = true;
            await Task.Delay(1);
            return (VsaResult<int>)99;
        });

        // Assert
        fallbackExecuted.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task OrElseAsync_WithTaskFallback_ShouldWork()
    {
        // Arrange
        VsaResult<int> errorOrInt = Error.NotFound();
        Task<VsaResult<int>> fallbackTask = Task.FromResult<VsaResult<int>>(99);

        // Act
        var result = await errorOrInt.OrElseAsync(fallbackTask);

        // Assert
        result.Value.Should().Be(99);
    }

    [Fact]
    public async Task OrElse_OnTaskErrorOr_ShouldWorkCorrectly()
    {
        // Arrange
        Task<VsaResult<int>> taskErrorOr = Task.FromResult<VsaResult<int>>(Error.NotFound());

        // Act
        var result = await taskErrorOr.OrElse(_ => 42);

        // Assert
        result.Value.Should().Be(42);
    }

    [Fact]
    public void OrElse_ReceivesOriginalErrors()
    {
        // Arrange
        VsaResult<int> errorOrInt = new List<Error>
        {
            Error.Validation(code: "A"),
            Error.Validation(code: "B"),
        };
        List<Error>? receivedErrors = null;

        // Act
        var result = errorOrInt.OrElse(errors =>
        {
            receivedErrors = errors;
            return 42;
        });

        // Assert
        receivedErrors.Should().HaveCount(2);
        receivedErrors![0].Code.Should().Be("A");
        receivedErrors[1].Code.Should().Be("B");
    }
}
