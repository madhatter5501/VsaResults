using FluentAssertions;
using VsaResults;

namespace Tests;

public class CombineTests
{
    [Fact]
    public void Combine_WhenBothSucceed_ShouldReturnTuple()
    {
        // Arrange
        VsaResult<int> first = 1;
        VsaResult<string> second = "two";

        // Act
        VsaResult<(int First, string Second)> result = VsaResultCombine.Combine(first, second);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be((1, "two"));
    }

    [Fact]
    public void Combine_WhenFirstFails_ShouldReturnErrors()
    {
        // Arrange
        VsaResult<int> first = Error.NotFound();
        VsaResult<string> second = "two";

        // Act
        VsaResult<(int First, string Second)> result = VsaResultCombine.Combine(first, second);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Combine_WhenSecondFails_ShouldReturnErrors()
    {
        // Arrange
        VsaResult<int> first = 1;
        VsaResult<string> second = Error.Validation();

        // Act
        VsaResult<(int First, string Second)> result = VsaResultCombine.Combine(first, second);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.FirstError.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Combine_WhenBothFail_ShouldReturnAllErrors()
    {
        // Arrange
        VsaResult<int> first = Error.NotFound();
        VsaResult<string> second = Error.Validation();

        // Act
        VsaResult<(int First, string Second)> result = VsaResultCombine.Combine(first, second);

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
        VsaResult<int> first = 1;
        VsaResult<string> second = "two";
        VsaResult<double> third = 3.0;

        // Act
        VsaResult<(int First, string Second, double Third)> result = VsaResultCombine.Combine(first, second, third);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be((1, "two", 3.0));
    }

    [Fact]
    public void Combine_ThreeResults_WhenSomeFail_ShouldReturnAllErrors()
    {
        // Arrange
        VsaResult<int> first = Error.NotFound();
        VsaResult<string> second = "two";
        VsaResult<double> third = Error.Validation();

        // Act
        VsaResult<(int First, string Second, double Third)> result = VsaResultCombine.Combine(first, second, third);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Collect_WhenAllSucceed_ShouldReturnList()
    {
        // Arrange
        var results = new List<VsaResult<int>>
        {
            1,
            2,
            3,
        };

        // Act
        VsaResult<List<int>> result = VsaResultCombine.Collect(results);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Collect_WhenSomeFail_ShouldReturnAllErrors()
    {
        // Arrange
        var results = new List<VsaResult<int>>
        {
            1,
            Error.NotFound(),
            3,
            Error.Validation(),
        };

        // Act
        VsaResult<List<int>> result = VsaResultCombine.Collect(results);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Collect_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var results = new List<VsaResult<int>>();

        // Act
        VsaResult<List<int>> result = VsaResultCombine.Collect(results);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Collect_WithParams_WhenAllSucceed_ShouldReturnList()
    {
        // Arrange & Act
        VsaResult<List<int>> result = VsaResultCombine.Collect<int>(1, 2, 3);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CombineAsync_WhenBothSucceed_ShouldReturnTuple()
    {
        // Arrange
        Task<VsaResult<int>> firstTask = Task.FromResult<VsaResult<int>>(1);
        Task<VsaResult<string>> secondTask = Task.FromResult<VsaResult<string>>("two");

        // Act
        VsaResult<(int First, string Second)> result = await VsaResultExtensions.Combine(firstTask, secondTask);

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
            Task.FromResult<VsaResult<int>>(1),
            Task.FromResult<VsaResult<int>>(2),
            Task.FromResult<VsaResult<int>>(3),
        };

        // Act
        VsaResult<List<int>> result = await VsaResultExtensions.CollectAsync(tasks);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Combine_SixResults_WhenAllSucceed_ShouldReturnTuple()
    {
        // Arrange
        VsaResult<int> first = 1;
        VsaResult<string> second = "two";
        VsaResult<double> third = 3.0;
        VsaResult<bool> fourth = true;
        VsaResult<char> fifth = 'e';
        VsaResult<long> sixth = 6L;

        // Act
        var result = VsaResultCombine.Combine(first, second, third, fourth, fifth, sixth);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be((1, "two", 3.0, true, 'e', 6L));
    }

    [Fact]
    public void Combine_SixResults_WhenSomeFail_ShouldReturnAllErrors()
    {
        // Arrange
        VsaResult<int> first = Error.NotFound();
        VsaResult<string> second = "two";
        VsaResult<double> third = Error.Validation();
        VsaResult<bool> fourth = true;
        VsaResult<char> fifth = Error.Conflict();
        VsaResult<long> sixth = 6L;

        // Act
        var result = VsaResultCombine.Combine(first, second, third, fourth, fifth, sixth);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void Combine_SevenResults_WhenAllSucceed_ShouldReturnTuple()
    {
        // Arrange
        VsaResult<int> first = 1;
        VsaResult<string> second = "two";
        VsaResult<double> third = 3.0;
        VsaResult<bool> fourth = true;
        VsaResult<char> fifth = 'e';
        VsaResult<long> sixth = 6L;
        VsaResult<float> seventh = 7.0f;

        // Act
        var result = VsaResultCombine.Combine(first, second, third, fourth, fifth, sixth, seventh);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be((1, "two", 3.0, true, 'e', 6L, 7.0f));
    }

    [Fact]
    public void Combine_SevenResults_WhenSomeFail_ShouldReturnAllErrors()
    {
        // Arrange
        VsaResult<int> first = 1;
        VsaResult<string> second = Error.Validation(code: "V1");
        VsaResult<double> third = 3.0;
        VsaResult<bool> fourth = Error.Validation(code: "V2");
        VsaResult<char> fifth = 'e';
        VsaResult<long> sixth = 6L;
        VsaResult<float> seventh = Error.Validation(code: "V3");

        // Act
        var result = VsaResultCombine.Combine(first, second, third, fourth, fifth, sixth, seventh);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(3);
        result.Errors.Select(e => e.Code).Should().BeEquivalentTo(new[] { "V1", "V2", "V3" });
    }

    [Fact]
    public void Combine_EightResults_WhenAllSucceed_ShouldReturnTuple()
    {
        // Arrange
        VsaResult<int> first = 1;
        VsaResult<string> second = "two";
        VsaResult<double> third = 3.0;
        VsaResult<bool> fourth = true;
        VsaResult<char> fifth = 'e';
        VsaResult<long> sixth = 6L;
        VsaResult<float> seventh = 7.0f;
        VsaResult<byte> eighth = 8;

        // Act
        var result = VsaResultCombine.Combine(first, second, third, fourth, fifth, sixth, seventh, eighth);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be((1, "two", 3.0, true, 'e', 6L, 7.0f, (byte)8));
    }

    [Fact]
    public void Combine_EightResults_WhenAllFail_ShouldReturnAllErrors()
    {
        // Arrange
        VsaResult<int> first = Error.Failure(code: "E1");
        VsaResult<string> second = Error.Failure(code: "E2");
        VsaResult<double> third = Error.Failure(code: "E3");
        VsaResult<bool> fourth = Error.Failure(code: "E4");
        VsaResult<char> fifth = Error.Failure(code: "E5");
        VsaResult<long> sixth = Error.Failure(code: "E6");
        VsaResult<float> seventh = Error.Failure(code: "E7");
        VsaResult<byte> eighth = Error.Failure(code: "E8");

        // Act
        var result = VsaResultCombine.Combine(first, second, third, fourth, fifth, sixth, seventh, eighth);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(8);
    }
}
