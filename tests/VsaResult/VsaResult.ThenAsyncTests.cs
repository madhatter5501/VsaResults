using FluentAssertions;
using VsaResults;

namespace Tests;

public class ThenAsyncTests
{
    [Fact]
    public async Task CallingThenAsync_WhenIsSuccess_ShouldInvokeNextThen()
    {
        // Arrange
        VsaResult<string> errorOrString = "5";

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(num => Task.FromResult(num * 2))
            .ThenDoAsync(num => Task.Run(() => { _ = 5; }))
            .ThenAsync(Convert.ToStringAsync);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo("10");
    }

    [Fact]
    public async Task CallingThenAsync_WhenIsError_ShouldReturnErrors()
    {
        // Arrange
        VsaResult<string> errorOrString = Error.NotFound();

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .ThenAsync(num => Task.FromResult(num * 2))
            .ThenDoAsync(num => Task.Run(() => { _ = 5; }))
            .ThenAsync(Convert.ToStringAsync);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().BeEquivalentTo(errorOrString.FirstError);
    }
}
