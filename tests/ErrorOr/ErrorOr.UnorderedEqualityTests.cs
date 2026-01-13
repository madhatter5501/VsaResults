using FluentAssertions;
using VsaResults;

namespace Tests;

public class UnorderedEqualityTests
{
    [Fact]
    public void Equals_WithSameErrorsDifferentOrder_ShouldReturnTrue()
    {
        // Arrange
        var comparer = ErrorOrUnorderedEqualityComparer<int>.Instance;
        ErrorOr<int> result1 = new List<Error>
        {
            Error.Validation(code: "A"),
            Error.Validation(code: "B"),
            Error.Validation(code: "C"),
        };
        ErrorOr<int> result2 = new List<Error>
        {
            Error.Validation(code: "C"),
            Error.Validation(code: "A"),
            Error.Validation(code: "B"),
        };

        // Act
        var areEqual = comparer.Equals(result1, result2);

        // Assert
        areEqual.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentErrors_ShouldReturnFalse()
    {
        // Arrange
        var comparer = ErrorOrUnorderedEqualityComparer<int>.Instance;
        ErrorOr<int> result1 = new List<Error>
        {
            Error.Validation(code: "A"),
            Error.Validation(code: "B"),
        };
        ErrorOr<int> result2 = new List<Error>
        {
            Error.Validation(code: "A"),
            Error.Validation(code: "C"),
        };

        // Act
        var areEqual = comparer.Equals(result1, result2);

        // Assert
        areEqual.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var comparer = ErrorOrUnorderedEqualityComparer<int>.Instance;
        ErrorOr<int> result1 = 42;
        ErrorOr<int> result2 = 42;

        // Act
        var areEqual = comparer.Equals(result1, result2);

        // Assert
        areEqual.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var comparer = ErrorOrUnorderedEqualityComparer<int>.Instance;
        ErrorOr<int> result1 = 42;
        ErrorOr<int> result2 = 99;

        // Act
        var areEqual = comparer.Equals(result1, result2);

        // Assert
        areEqual.Should().BeFalse();
    }

    [Fact]
    public void Equals_SuccessAndError_ShouldReturnFalse()
    {
        // Arrange
        var comparer = ErrorOrUnorderedEqualityComparer<int>.Instance;
        ErrorOr<int> success = 42;
        ErrorOr<int> error = Error.NotFound();

        // Act
        var areEqual = comparer.Equals(success, error);

        // Assert
        areEqual.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDuplicateErrors_ShouldCompareCorrectly()
    {
        // Arrange
        var comparer = ErrorOrUnorderedEqualityComparer<int>.Instance;
        ErrorOr<int> result1 = new List<Error>
        {
            Error.Validation(code: "A"),
            Error.Validation(code: "A"),
            Error.Validation(code: "B"),
        };
        ErrorOr<int> result2 = new List<Error>
        {
            Error.Validation(code: "B"),
            Error.Validation(code: "A"),
            Error.Validation(code: "A"),
        };

        // Act
        var areEqual = comparer.Equals(result1, result2);

        // Assert
        areEqual.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentDuplicateCounts_ShouldReturnFalse()
    {
        // Arrange
        var comparer = ErrorOrUnorderedEqualityComparer<int>.Instance;
        ErrorOr<int> result1 = new List<Error>
        {
            Error.Validation(code: "A"),
            Error.Validation(code: "A"),
            Error.Validation(code: "B"),
        };
        ErrorOr<int> result2 = new List<Error>
        {
            Error.Validation(code: "A"),
            Error.Validation(code: "B"),
            Error.Validation(code: "B"),
        };

        // Act
        var areEqual = comparer.Equals(result1, result2);

        // Assert
        areEqual.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameErrorsDifferentOrder_ShouldReturnSameHash()
    {
        // Arrange
        var comparer = ErrorOrUnorderedEqualityComparer<int>.Instance;
        ErrorOr<int> result1 = new List<Error>
        {
            Error.Validation(code: "A"),
            Error.Validation(code: "B"),
        };
        ErrorOr<int> result2 = new List<Error>
        {
            Error.Validation(code: "B"),
            Error.Validation(code: "A"),
        };

        // Act
        var hash1 = comparer.GetHashCode(result1);
        var hash2 = comparer.GetHashCode(result2);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_ForSuccessValue_ShouldReturnValueHashCode()
    {
        // Arrange
        var comparer = ErrorOrUnorderedEqualityComparer<int>.Instance;
        ErrorOr<int> result = 42;

        // Act
        var hash = comparer.GetHashCode(result);

        // Assert
        hash.Should().Be(42.GetHashCode());
    }

    [Fact]
    public void Instance_ShouldBeSingleton()
    {
        // Arrange & Act
        var instance1 = ErrorOrUnorderedEqualityComparer<int>.Instance;
        var instance2 = ErrorOrUnorderedEqualityComparer<int>.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void CanBeUsedWithLinqDistinct()
    {
        // Arrange
        var comparer = ErrorOrUnorderedEqualityComparer<int>.Instance;
        var results = new List<ErrorOr<int>>
        {
            new List<Error> { Error.Validation(code: "A"), Error.Validation(code: "B") },
            new List<Error> { Error.Validation(code: "B"), Error.Validation(code: "A") },  // Same as first, different order
            new List<Error> { Error.Validation(code: "C") },
        };

        // Act
        var distinct = results.Distinct(comparer).ToList();

        // Assert
        distinct.Should().HaveCount(2);
    }

    [Fact]
    public void CanBeUsedWithDictionary()
    {
        // Arrange
        var comparer = ErrorOrUnorderedEqualityComparer<int>.Instance;
        var dict = new Dictionary<ErrorOr<int>, string>(comparer);

        ErrorOr<int> key1 = new List<Error> { Error.Validation(code: "A"), Error.Validation(code: "B") };
        ErrorOr<int> key2 = new List<Error> { Error.Validation(code: "B"), Error.Validation(code: "A") };

        // Act
        dict[key1] = "First";

        // Assert
        dict.Should().ContainKey(key2);
        dict[key2].Should().Be("First");
    }
}
