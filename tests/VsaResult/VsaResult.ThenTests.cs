using FluentAssertions;
using VsaResults;

namespace Tests;

public class ThenTests
{
    [Fact]
    public void CallingThen_WhenIsSuccess_ShouldInvokeGivenFunc()
    {
        // Arrange
        VsaResult<string> errorOrString = "5";

        // Act
        VsaResult<string> result = errorOrString
            .Then(Convert.ToInt)
            .Then(num => num * 2)
            .Then(Convert.ToString);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo("10");
    }

    [Fact]
    public void CallingThen_WhenIsSuccess_ShouldInvokeGivenAction()
    {
        // Arrange
        VsaResult<string> errorOrString = "5";

        // Act
        VsaResult<int> result = errorOrString
            .ThenDo(str => { _ = 5; })
            .Then(Convert.ToInt)
            .ThenDo(str => { _ = 5; });

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(5);
    }

    [Fact]
    public void CallingThen_WhenIsError_ShouldReturnErrors()
    {
        // Arrange
        VsaResult<string> errorOrString = Error.NotFound();

        // Act
        VsaResult<string> result = errorOrString
            .Then(Convert.ToInt)
            .Then(num => num * 2)
            .ThenDo(str => { _ = 5; })
            .Then(Convert.ToString);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().BeEquivalentTo(errorOrString.FirstError);
    }

    [Fact]
    public async Task CallingThenAfterThenAsync_WhenIsSuccess_ShouldInvokeGivenFunc()
    {
        // Arrange
        VsaResult<string> errorOrString = "5";

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .Then(num => num * 2)
            .ThenAsync(Convert.ToStringAsync)
            .Then(Convert.ToInt)
            .ThenAsync(Convert.ToStringAsync)
            .ThenDo(num => { _ = 5; });

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("10");
    }

    [Fact]
    public async Task CallingThenAfterThenAsync_WhenIsError_ShouldReturnErrors()
    {
        // Arrange
        VsaResult<string> errorOrString = Error.NotFound();

        // Act
        VsaResult<string> result = await errorOrString
            .ThenAsync(Convert.ToIntAsync)
            .Then(Convert.ToString);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().BeEquivalentTo(errorOrString.FirstError);
    }
}
