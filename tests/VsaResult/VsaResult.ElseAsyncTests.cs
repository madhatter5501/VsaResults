using FluentAssertions;
using VsaResults;

namespace Tests;

public class ElseAsyncTests
{
    [Fact]
    public async Task CallingElseAsyncWithValueFunc_WhenIsSuccess_ShouldNotInvokeElseFunc()
    {
        // Arrange
        VsaResult<string> errorOrString = "5";

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(errors => Task.FromResult($"Error count: {errors.Count}"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(errorOrString.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithValueFunc_WhenIsError_ShouldInvokeElseFunc()
    {
        // Arrange
        VsaResult<string> errorOrString = Error.NotFound();

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(errors => Task.FromResult($"Error count: {errors.Count}"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo("Error count: 1");
    }

    [Fact]
    public async Task CallingElseAsyncWithValue_WhenIsSuccess_ShouldNotReturnElseValue()
    {
        // Arrange
        VsaResult<string> errorOrString = "5";

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(Task.FromResult("oh no"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(errorOrString.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithValue_WhenIsError_ShouldInvokeElseFunc()
    {
        // Arrange
        VsaResult<string> errorOrString = Error.NotFound();

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(Task.FromResult("oh no"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo("oh no");
    }

    [Fact]
    public async Task CallingElseAsyncWithError_WhenIsError_ShouldReturnElseError()
    {
        // Arrange
        VsaResult<string> errorOrString = Error.NotFound();

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(Task.FromResult(Error.Unexpected()));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Unexpected);
    }

    [Fact]
    public async Task CallingElseAsyncWithError_WhenIsSuccess_ShouldNotReturnElseError()
    {
        // Arrange
        VsaResult<string> errorOrString = "5";

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(Task.FromResult(Error.Unexpected()));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(errorOrString.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorFunc_WhenIsError_ShouldReturnElseError()
    {
        // Arrange
        VsaResult<string> errorOrString = Error.NotFound();

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(errors => Task.FromResult(Error.Unexpected()));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Unexpected);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorFunc_WhenIsSuccess_ShouldNotReturnElseError()
    {
        // Arrange
        VsaResult<string> errorOrString = "5";

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(errors => Task.FromResult(Error.Unexpected()));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(errorOrString.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorFunc_WhenIsError_ShouldReturnElseErrors()
    {
        // Arrange
        VsaResult<string> errorOrString = Error.NotFound();

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(errors => Task.FromResult(new List<Error> { Error.Unexpected() }));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Unexpected);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorFunc_WhenIsSuccess_ShouldNotReturnElseErrors()
    {
        // Arrange
        VsaResult<string> errorOrString = "5";

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(Convert.ToStringAsync)
            .ElseAsync(errors => Task.FromResult(new List<Error> { Error.Unexpected() }));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(errorOrString.Value);
    }
}
