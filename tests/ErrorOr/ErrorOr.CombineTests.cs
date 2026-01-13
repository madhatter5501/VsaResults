using FluentAssertions;
using VsaResults;

namespace Tests;

public class CombineTests
{
    [Fact]
    public void Combine_WhenBothSucceed_ShouldReturnTuple()
    {
        // Arrange
        ErrorOr<int> first = 1;
        ErrorOr<string> second = "two";

        // Act
        ErrorOr<(int First, string Second)> result = ErrorOrCombine.Combine(first, second);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be((1, "two"));
    }

    [Fact]
    public void Combine_WhenFirstFails_ShouldReturnErrors()
    {
        // Arrange
        ErrorOr<int> first = Error.NotFound();
        ErrorOr<string> second = "two";

        // Act
        ErrorOr<(int First, string Second)> result = ErrorOrCombine.Combine(first, second);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Combine_WhenSecondFails_ShouldReturnErrors()
    {
        // Arrange
        ErrorOr<int> first = 1;
        ErrorOr<string> second = Error.Validation();

        // Act
        ErrorOr<(int First, string Second)> result = ErrorOrCombine.Combine(first, second);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.FirstError.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Combine_WhenBothFail_ShouldReturnAllErrors()
    {
        // Arrange
        ErrorOr<int> first = Error.NotFound();
        ErrorOr<string> second = Error.Validation();

        // Act
        ErrorOr<(int First, string Second)> result = ErrorOrCombine.Combine(first, second);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors[0].Type.Should().Be(ErrorType.NotFound);
        result.Errors[1].Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Combine_ThreeResults_WhenAllSucceed_ShouldReturnTuple()
    {
        // Arrange
        ErrorOr<int> first = 1;
        ErrorOr<string> second = "two";
        ErrorOr<double> third = 3.0;

        // Act
        ErrorOr<(int First, string Second, double Third)> result = ErrorOrCombine.Combine(first, second, third);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be((1, "two", 3.0));
    }

    [Fact]
    public void Combine_ThreeResults_WhenSomeFail_ShouldReturnAllErrors()
    {
        // Arrange
        ErrorOr<int> first = Error.NotFound();
        ErrorOr<string> second = "two";
        ErrorOr<double> third = Error.Validation();

        // Act
        ErrorOr<(int First, string Second, double Third)> result = ErrorOrCombine.Combine(first, second, third);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Collect_WhenAllSucceed_ShouldReturnList()
    {
        // Arrange
        var results = new List<ErrorOr<int>>
        {
            1,
            2,
            3,
        };

        // Act
        ErrorOr<List<int>> result = ErrorOrCombine.Collect(results);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Collect_WhenSomeFail_ShouldReturnAllErrors()
    {
        // Arrange
        var results = new List<ErrorOr<int>>
        {
            1,
            Error.NotFound(),
            3,
            Error.Validation(),
        };

        // Act
        ErrorOr<List<int>> result = ErrorOrCombine.Collect(results);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Collect_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var results = new List<ErrorOr<int>>();

        // Act
        ErrorOr<List<int>> result = ErrorOrCombine.Collect(results);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Collect_WithParams_WhenAllSucceed_ShouldReturnList()
    {
        // Arrange & Act
        ErrorOr<List<int>> result = ErrorOrCombine.Collect<int>(1, 2, 3);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CombineAsync_WhenBothSucceed_ShouldReturnTuple()
    {
        // Arrange
        Task<ErrorOr<int>> firstTask = Task.FromResult<ErrorOr<int>>(1);
        Task<ErrorOr<string>> secondTask = Task.FromResult<ErrorOr<string>>("two");

        // Act
        ErrorOr<(int First, string Second)> result = await ErrorOrExtensions.Combine(firstTask, secondTask);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be((1, "two"));
    }

    [Fact]
    public async Task CollectAsync_WhenAllSucceed_ShouldReturnList()
    {
        // Arrange
        var tasks = new[]
        {
            Task.FromResult<ErrorOr<int>>(1),
            Task.FromResult<ErrorOr<int>>(2),
            Task.FromResult<ErrorOr<int>>(3),
        };

        // Act
        ErrorOr<List<int>> result = await ErrorOrExtensions.CollectAsync(tasks);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Combine_SixResults_WhenAllSucceed_ShouldReturnTuple()
    {
        // Arrange
        ErrorOr<int> first = 1;
        ErrorOr<string> second = "two";
        ErrorOr<double> third = 3.0;
        ErrorOr<bool> fourth = true;
        ErrorOr<char> fifth = 'e';
        ErrorOr<long> sixth = 6L;

        // Act
        var result = ErrorOrCombine.Combine(first, second, third, fourth, fifth, sixth);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be((1, "two", 3.0, true, 'e', 6L));
    }

    [Fact]
    public void Combine_SixResults_WhenSomeFail_ShouldReturnAllErrors()
    {
        // Arrange
        ErrorOr<int> first = Error.NotFound();
        ErrorOr<string> second = "two";
        ErrorOr<double> third = Error.Validation();
        ErrorOr<bool> fourth = true;
        ErrorOr<char> fifth = Error.Conflict();
        ErrorOr<long> sixth = 6L;

        // Act
        var result = ErrorOrCombine.Combine(first, second, third, fourth, fifth, sixth);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void Combine_SevenResults_WhenAllSucceed_ShouldReturnTuple()
    {
        // Arrange
        ErrorOr<int> first = 1;
        ErrorOr<string> second = "two";
        ErrorOr<double> third = 3.0;
        ErrorOr<bool> fourth = true;
        ErrorOr<char> fifth = 'e';
        ErrorOr<long> sixth = 6L;
        ErrorOr<float> seventh = 7.0f;

        // Act
        var result = ErrorOrCombine.Combine(first, second, third, fourth, fifth, sixth, seventh);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be((1, "two", 3.0, true, 'e', 6L, 7.0f));
    }

    [Fact]
    public void Combine_SevenResults_WhenSomeFail_ShouldReturnAllErrors()
    {
        // Arrange
        ErrorOr<int> first = 1;
        ErrorOr<string> second = Error.Validation(code: "V1");
        ErrorOr<double> third = 3.0;
        ErrorOr<bool> fourth = Error.Validation(code: "V2");
        ErrorOr<char> fifth = 'e';
        ErrorOr<long> sixth = 6L;
        ErrorOr<float> seventh = Error.Validation(code: "V3");

        // Act
        var result = ErrorOrCombine.Combine(first, second, third, fourth, fifth, sixth, seventh);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(3);
        result.Errors.Select(e => e.Code).Should().BeEquivalentTo(new[] { "V1", "V2", "V3" });
    }

    [Fact]
    public void Combine_EightResults_WhenAllSucceed_ShouldReturnTuple()
    {
        // Arrange
        ErrorOr<int> first = 1;
        ErrorOr<string> second = "two";
        ErrorOr<double> third = 3.0;
        ErrorOr<bool> fourth = true;
        ErrorOr<char> fifth = 'e';
        ErrorOr<long> sixth = 6L;
        ErrorOr<float> seventh = 7.0f;
        ErrorOr<byte> eighth = 8;

        // Act
        var result = ErrorOrCombine.Combine(first, second, third, fourth, fifth, sixth, seventh, eighth);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be((1, "two", 3.0, true, 'e', 6L, 7.0f, (byte)8));
    }

    [Fact]
    public void Combine_EightResults_WhenAllFail_ShouldReturnAllErrors()
    {
        // Arrange
        ErrorOr<int> first = Error.Failure(code: "E1");
        ErrorOr<string> second = Error.Failure(code: "E2");
        ErrorOr<double> third = Error.Failure(code: "E3");
        ErrorOr<bool> fourth = Error.Failure(code: "E4");
        ErrorOr<char> fifth = Error.Failure(code: "E5");
        ErrorOr<long> sixth = Error.Failure(code: "E6");
        ErrorOr<float> seventh = Error.Failure(code: "E7");
        ErrorOr<byte> eighth = Error.Failure(code: "E8");

        // Act
        var result = ErrorOrCombine.Combine(first, second, third, fourth, fifth, sixth, seventh, eighth);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(8);
    }
}
