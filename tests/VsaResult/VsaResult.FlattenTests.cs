using FluentAssertions;
using VsaResults;

namespace Tests;

public class FlattenTests
{
    [Fact]
    public void Flatten_WhenOuterSuccess_InnerSuccess_ShouldReturnInnerValue()
    {
        // Arrange
        VsaResult<VsaResult<int>> nested = (VsaResult<int>)42;

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
        VsaResult<VsaResult<int>> nested = (VsaResult<int>)Error.NotFound(code: "Inner.Error");

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
        VsaResult<VsaResult<int>> nested = Error.Validation(code: "Outer.Error");

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
        Task<VsaResult<VsaResult<int>>> nested = Task.FromResult<VsaResult<VsaResult<int>>>((VsaResult<int>)42);

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
        VsaResult<Task<VsaResult<int>>> nested = Task.FromResult<VsaResult<int>>(42);

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
        var taskThatShouldNotRun = new Func<Task<VsaResult<int>>>(() =>
            throw new InvalidOperationException("Task should not be awaited"));

        VsaResult<Task<VsaResult<int>>> nested = Error.Validation(code: "Outer.Error");

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
        VsaResult<VsaResult<int>> nested = (VsaResult<int>)new List<Error>
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
        VsaResult<int> initial = 5;

        // Act - simulating a Then that returns VsaResult<VsaResult<T>>
        var nested = initial.Then(val => (VsaResult<VsaResult<string>>)(VsaResult<string>)$"Value: {val}");
        var result = nested.Flatten();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("Value: 5");
    }
}
