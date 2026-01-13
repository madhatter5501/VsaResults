using FluentAssertions;
using VsaResults;

namespace Tests;

public class DeconstructTests
{
    [Fact]
    public void Deconstruct_WhenSuccess_ShouldReturnValueAndNullErrors()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 42;

        // Act
        var (value, errors) = errorOrInt;

        // Assert
        value.Should().Be(42);
        errors.Should().BeNull();
    }

    [Fact]
    public void Deconstruct_WhenError_ShouldReturnDefaultValueAndErrors()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Error.NotFound(code: "User.NotFound");

        // Act
        var (value, errors) = errorOrInt;

        // Assert
        value.Should().Be(default(int));
        errors.Should().NotBeNull();
        errors.Should().HaveCount(1);
        errors![0].Code.Should().Be("User.NotFound");
    }

    [Fact]
    public void Deconstruct_WithThreeParameters_WhenSuccess_ShouldReturnCorrectValues()
    {
        // Arrange
        ErrorOr<string> errorOrString = "Hello";

        // Act
        var (isError, value, errors) = errorOrString;

        // Assert
        isError.Should().BeFalse();
        value.Should().Be("Hello");
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Deconstruct_WithThreeParameters_WhenError_ShouldReturnCorrectValues()
    {
        // Arrange
        ErrorOr<string> errorOrString = new List<Error>
        {
            Error.Validation(code: "Field.A"),
            Error.Validation(code: "Field.B"),
        };

        // Act
        var (isError, value, errors) = errorOrString;

        // Assert
        isError.Should().BeTrue();
        value.Should().BeNull();
        errors.Should().HaveCount(2);
    }

    [Fact]
    public void Deconstruct_CanBeUsedInPatternMatching()
    {
        // Arrange
        ErrorOr<int> result = 42;

        // Act & Assert
        if (result is var (value, errors) && errors is null)
        {
            value.Should().Be(42);
        }
        else
        {
            Assert.Fail("Should have matched successful case");
        }
    }

    [Fact]
    public void Deconstruct_WithErrorList_CanIterateOverErrors()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new List<Error>
        {
            Error.Validation(code: "A"),
            Error.Validation(code: "B"),
            Error.Validation(code: "C"),
        };

        // Act
        var (_, errors) = errorOrInt;
        var codes = errors?.Select(e => e.Code).ToList();

        // Assert
        codes.Should().BeEquivalentTo(new[] { "A", "B", "C" });
    }

    [Fact]
    public void Deconstruct_WithReferenceType_WhenSuccess_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<List<int>> errorOrList = new List<int> { 1, 2, 3 };

        // Act
        var (value, errors) = errorOrList;

        // Assert
        value.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        errors.Should().BeNull();
    }

    [Fact]
    public void Deconstruct_WithReferenceType_WhenError_ShouldReturnNull()
    {
        // Arrange
        ErrorOr<List<int>> errorOrList = Error.NotFound();

        // Act
        var (value, errors) = errorOrList;

        // Assert
        value.Should().BeNull();
        errors.Should().NotBeNull();
    }
}
