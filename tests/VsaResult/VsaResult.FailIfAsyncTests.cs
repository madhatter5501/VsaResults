using FluentAssertions;
using VsaResults;

namespace Tests;

public class FailIfAsyncTests
{
    private record Person(string Name);

    [Fact]
    public async Task CallingFailIfAsync_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        VsaResult<int> errorOrInt = 5;

        // Act
        VsaResult<int> result = await errorOrInt
            .FailIfAsync(num => Task.FromResult(num > 3), Error.Failure());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task CallingFailIfAsyncExtensionMethod_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        VsaResult<int> errorOrInt = 5;

        // Act
        VsaResult<int> result = await errorOrInt
            .ThenAsync(num => Task.FromResult(num))
            .FailIfAsync(num => Task.FromResult(num > 3), Error.Failure());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task CallingFailIfAsync_WhenDoesNotFailIf_ShouldReturnValue()
    {
        // Arrange
        VsaResult<int> errorOrInt = 5;

        // Act
        VsaResult<int> result = await errorOrInt
            .FailIfAsync(num => Task.FromResult(num > 10), Error.Failure());

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(5);
    }

    [Fact]
    public async Task CallingFailIf_WhenIsError_ShouldNotInvokeFailIfFunc()
    {
        // Arrange
        VsaResult<string> errorOrString = Error.NotFound();

        // Act
        VsaResult<string> result = await errorOrString
            .FailIfAsync(str => Task.FromResult(str == string.Empty), Error.Failure());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task CallingFailIfAsyncWithErrorBuilder_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        VsaResult<int> errorOrInt = 5;

        // Act
        VsaResult<int> result = await errorOrInt
            .FailIfAsync(num => Task.FromResult(num > 3), (num) => Task.FromResult(Error.Failure(description: $"{num} is greater than 3.")));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Description.Should().Be("5 is greater than 3.");
    }

    [Fact]
    public async Task CallingFailIfAsyncExtensionMethodWithErrorBuilder_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        VsaResult<int> errorOrInt = 5;

        // Act
        VsaResult<int> result = await errorOrInt
            .ThenAsync(num => Task.FromResult(num))
            .FailIfAsync(num => Task.FromResult(num > 3), (num) => Task.FromResult(Error.Failure(description: $"{num} is greater than 3.")));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Description.Should().Be("5 is greater than 3.");
    }

    [Fact]
    public async Task CallingFailIfAsyncWithErrorBuilder_WhenDoesNotFailIf_ShouldReturnValue()
    {
        // Arrange
        VsaResult<int> errorOrInt = 5;

        // Act
        VsaResult<int> result = await errorOrInt
            .FailIfAsync(num => Task.FromResult(num > 10), (num) => Task.FromResult(Error.Failure(description: $"{num} is greater than 10.")));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(5);
    }

    [Fact]
    public async Task CallingFailIfWithErrorBuilder_WhenIsError_ShouldNotInvokeFailIfFunc()
    {
        // Arrange
        VsaResult<int> errorOrInt = Error.NotFound();

        // Act
        VsaResult<int> result = await errorOrInt
            .FailIfAsync(num => Task.FromResult(num > 3), (num) => Task.FromResult(Error.Failure(description: $"{num} is greater than 3.")));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }
}
