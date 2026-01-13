using ErrorOr;
using FluentAssertions;

namespace Tests;

public class TryTests
{
    [Fact]
    public void Try_WhenFunctionSucceeds_ShouldReturnValue()
    {
        // Arrange & Act
        ErrorOr<int> result = ErrorOr<int>.Try(() => 42);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Try_WhenFunctionThrows_ShouldReturnError()
    {
        // Arrange & Act
        ErrorOr<int> result = ErrorOr<int>.Try(() =>
        {
            throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162 // Unreachable code detected
            return 42;
#pragma warning restore CS0162
        });

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("InvalidOperationException");
        result.FirstError.Description.Should().Be("Test exception");
        result.FirstError.Type.Should().Be(ErrorType.Unexpected);
    }

    [Fact]
    public void Try_WhenFunctionThrowsWithCustomMapper_ShouldReturnMappedError()
    {
        // Arrange & Act
        ErrorOr<int> result = ErrorOr<int>.Try(
            () =>
            {
                throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162
                return 42;
#pragma warning restore CS0162
            },
            ex => Error.Validation(code: "Custom.Error", description: ex.Message));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Custom.Error");
        result.FirstError.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Try_WhenFunctionThrows_ShouldIncludeExceptionInMetadata()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");

        // Act
        ErrorOr<int> result = ErrorOr<int>.Try(() =>
        {
            throw expectedException;
#pragma warning disable CS0162
            return 42;
#pragma warning restore CS0162
        });

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Metadata.Should().ContainKey("Exception");
        result.FirstError.Metadata!["Exception"].Should().Be(expectedException);
    }

    [Fact]
    public async Task TryAsync_WhenFunctionSucceeds_ShouldReturnValue()
    {
        // Arrange & Act
        ErrorOr<int> result = await ErrorOr<int>.TryAsync(async () =>
        {
            await Task.Delay(1);
            return 42;
        });

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task TryAsync_WhenFunctionThrows_ShouldReturnError()
    {
        // Arrange & Act
        ErrorOr<int> result = await ErrorOr<int>.TryAsync(async () =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Async test exception");
#pragma warning disable CS0162
            return 42;
#pragma warning restore CS0162
        });

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("InvalidOperationException");
        result.FirstError.Description.Should().Be("Async test exception");
    }

    [Fact]
    public async Task TryAsync_WhenFunctionThrowsWithCustomMapper_ShouldReturnMappedError()
    {
        // Arrange & Act
        ErrorOr<int> result = await ErrorOr<int>.TryAsync(
            async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Async test exception");
#pragma warning disable CS0162
                return 42;
#pragma warning restore CS0162
            },
            ex => Error.BadRequest(code: "Custom.AsyncError", description: ex.Message));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Custom.AsyncError");
        result.FirstError.Type.Should().Be(ErrorType.BadRequest);
    }

    [Fact]
    public void ErrorOrFactory_Try_WhenFunctionSucceeds_ShouldReturnValue()
    {
        // Arrange & Act
        ErrorOr<int> result = ErrorOrFactory.Try(() => 42);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task ErrorOrFactory_TryAsync_WhenFunctionSucceeds_ShouldReturnValue()
    {
        // Arrange & Act
        ErrorOr<int> result = await ErrorOrFactory.TryAsync(async () =>
        {
            await Task.Delay(1);
            return 42;
        });

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }
}
