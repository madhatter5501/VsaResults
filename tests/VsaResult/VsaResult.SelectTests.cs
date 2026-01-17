using FluentAssertions;
using VsaResults;

namespace Tests;

public class SelectTests
{
    [Fact]
    public void Select_WhenSuccess_ShouldProjectValue()
    {
        // Arrange
        VsaResult<int> errorOr = 5;

        // Act
        VsaResult<string> result = errorOr.Select(x => x.ToString());

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("5");
    }

    [Fact]
    public void Select_WhenError_ShouldReturnErrors()
    {
        // Arrange
        VsaResult<int> errorOr = Error.NotFound();

        // Act
        VsaResult<string> result = errorOr.Select(x => x.ToString());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void SelectMany_WhenSuccess_ShouldFlatten()
    {
        // Arrange
        VsaResult<int> errorOr = 5;

        // Act
        VsaResult<string> result = errorOr.SelectMany(x => VsaResultFactory.From(x.ToString()));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("5");
    }

    [Fact]
    public void SelectMany_WhenSuccess_AndInnerFails_ShouldReturnInnerError()
    {
        // Arrange
        VsaResult<int> errorOr = 5;

        // Act
        VsaResult<string> result = errorOr.SelectMany<string>(_ => Error.Validation());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void SelectMany_WhenError_ShouldReturnOriginalErrors()
    {
        // Arrange
        VsaResult<int> errorOr = Error.NotFound();

        // Act
        VsaResult<string> result = errorOr.SelectMany(x => VsaResultFactory.From(x.ToString()));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Where_WhenPredicatePasses_ShouldReturnOriginalValue()
    {
        // Arrange
        VsaResult<int> errorOr = 5;

        // Act
        VsaResult<int> result = errorOr.Where(x => x > 0, Error.Validation());

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(5);
    }

    [Fact]
    public void Where_WhenPredicateFails_ShouldReturnError()
    {
        // Arrange
        VsaResult<int> errorOr = -5;

        // Act
        VsaResult<int> result = errorOr.Where(x => x > 0, Error.Validation("Number.Negative", "Number must be positive"));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Number.Negative");
    }

    [Fact]
    public void Where_WhenAlreadyError_ShouldReturnOriginalError()
    {
        // Arrange
        VsaResult<int> errorOr = Error.NotFound();

        // Act
        VsaResult<int> result = errorOr.Where(x => x > 0, Error.Validation());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Where_WithErrorFactory_WhenPredicateFails_ShouldUseFactory()
    {
        // Arrange
        VsaResult<int> errorOr = -5;

        // Act
        VsaResult<int> result = errorOr.Where(
            x => x > 0,
            x => Error.Validation("Number.Negative", $"Number {x} must be positive"));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Description.Should().Be("Number -5 must be positive");
    }

    [Fact]
    public async Task Select_OnTask_WhenSuccess_ShouldProjectValue()
    {
        // Arrange
        Task<VsaResult<int>> errorOrTask = Task.FromResult<VsaResult<int>>(5);

        // Act
        VsaResult<string> result = await errorOrTask.Select(x => x.ToString());

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("5");
    }

    [Fact]
    public async Task SelectAsync_WhenSuccess_ShouldProjectValueAsync()
    {
        // Arrange
        VsaResult<int> errorOr = 5;

        // Act
        VsaResult<string> result = await errorOr.SelectAsync(async x =>
        {
            await Task.Delay(1);
            return x.ToString();
        });

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("5");
    }

    [Fact]
    public async Task SelectManyAsync_WhenSuccess_ShouldFlatten()
    {
        // Arrange
        VsaResult<int> errorOr = 5;

        // Act
        VsaResult<string> result = await errorOr.SelectManyAsync(async x =>
        {
            await Task.Delay(1);
            return VsaResultFactory.From(x.ToString());
        });

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("5");
    }

    [Fact]
    public async Task WhereAsync_WhenPredicatePasses_ShouldReturnOriginalValue()
    {
        // Arrange
        VsaResult<int> errorOr = 5;

        // Act
        VsaResult<int> result = await errorOr.WhereAsync(
            async x =>
            {
                await Task.Delay(1);
                return x > 0;
            },
            Error.Validation());

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(5);
    }

    [Fact]
    public async Task WhereAsync_WhenPredicateFails_ShouldReturnError()
    {
        // Arrange
        VsaResult<int> errorOr = -5;

        // Act
        VsaResult<int> result = await errorOr.WhereAsync(
            async x =>
            {
                await Task.Delay(1);
                return x > 0;
            },
            Error.Validation());

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
    }
}
