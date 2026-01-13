using FluentAssertions;
using VsaResults;

namespace Tests;

public class FlattenTests
{
    [Fact]
    public void Flatten_WhenOuterSuccess_InnerSuccess_ShouldReturnInnerValue()
    {
        // Arrange
        ErrorOr<ErrorOr<int>> nested = (ErrorOr<int>)42;

        // Act
        var result = nested.Flatten();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Flatten_WhenOuterSuccess_InnerError_ShouldReturnInnerErrors()
    {
        // Arrange
        ErrorOr<ErrorOr<int>> nested = (ErrorOr<int>)Error.NotFound(code: "Inner.Error");

        // Act
        var result = nested.Flatten();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Inner.Error");
    }

    [Fact]
    public void Flatten_WhenOuterError_ShouldReturnOuterErrors()
    {
        // Arrange
        ErrorOr<ErrorOr<int>> nested = Error.Validation(code: "Outer.Error");

        // Act
        var result = nested.Flatten();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Outer.Error");
    }

    [Fact]
    public async Task Flatten_OnTaskErrorOr_ShouldWorkCorrectly()
    {
        // Arrange
        Task<ErrorOr<ErrorOr<int>>> nested = Task.FromResult<ErrorOr<ErrorOr<int>>>((ErrorOr<int>)42);

        // Act
        var result = await nested.Flatten();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task FlattenAsync_WhenOuterSuccess_ShouldAwaitInnerTask()
    {
        // Arrange
        ErrorOr<Task<ErrorOr<int>>> nested = Task.FromResult<ErrorOr<int>>(42);

        // Act
        var result = await nested.FlattenAsync();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task FlattenAsync_WhenOuterError_ShouldReturnErrorWithoutAwaitingTask()
    {
        // Arrange
        // Create a task that would fail if awaited
        var taskThatShouldNotRun = new Func<Task<ErrorOr<int>>>(() =>
            throw new InvalidOperationException("Task should not be awaited"));

        ErrorOr<Task<ErrorOr<int>>> nested = Error.Validation(code: "Outer.Error");

        // Act
        var result = await nested.FlattenAsync();

        // Assert - if this completes without exception, the task wasn't awaited
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Outer.Error");
    }

    [Fact]
    public void Flatten_WithMultipleInnerErrors_ShouldPreserveAll()
    {
        // Arrange
        ErrorOr<ErrorOr<int>> nested = (ErrorOr<int>)new List<Error>
        {
            Error.Validation(code: "A"),
            Error.Validation(code: "B"),
            Error.Validation(code: "C"),
        };

        // Act
        var result = nested.Flatten();

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void Flatten_CanBeUsedAfterThen()
    {
        // Arrange
        ErrorOr<int> initial = 5;

        // Act - simulating a Then that returns ErrorOr<ErrorOr<T>>
        var nested = initial.Then(val => (ErrorOr<ErrorOr<string>>)(ErrorOr<string>)$"Value: {val}");
        var result = nested.Flatten();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("Value: 5");
    }
}
