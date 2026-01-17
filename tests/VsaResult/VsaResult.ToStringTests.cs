using FluentAssertions;
using VsaResults;

namespace Tests;

public class ToStringTests
{
    [Fact]
    public void ErrorOr_ToString_WhenSuccess_ShouldReturnFormattedString()
    {
        // Arrange
        VsaResult<int> result = 42;

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("IsError = False");
        str.Should().Contain("Value = 42");
    }

    [Fact]
    public void ErrorOr_ToString_WhenSuccessWithLongValue_ShouldTruncateValue()
    {
        // Arrange
        VsaResult<string> result = new string('x', 100);

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("IsError = False");
        str.Should().Contain("...");
        str.Length.Should().BeLessThan(150);
    }

    [Fact]
    public void ErrorOr_ToString_WhenError_ShouldReturnFormattedString()
    {
        // Arrange
        VsaResult<int> result = Error.NotFound(code: "User.NotFound");

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("IsError = True");
        str.Should().Contain("User.NotFound");
    }

    [Fact]
    public void ErrorOr_ToString_WhenMultipleErrors_ShouldShowCountOfAdditionalErrors()
    {
        // Arrange
        VsaResult<int> result = new List<Error>
        {
            Error.Validation(code: "Field.Required"),
            Error.Validation(code: "Field.TooShort"),
            Error.Validation(code: "Field.Invalid"),
        };

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("IsError = True");
        str.Should().Contain("Field.Required");
        str.Should().Contain("+2 more");
    }

    [Fact]
    public void Error_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var error = Error.NotFound(code: "User.NotFound", description: "User was not found");

        // Act
        var str = error.ToString();

        // Assert
        str.Should().Contain("Code = User.NotFound");
        str.Should().Contain("Type = NotFound");
        str.Should().Contain("Description = User was not found");
    }

    [Fact]
    public void Error_ToString_WithLongDescription_ShouldTruncate()
    {
        // Arrange
        var longDescription = new string('x', 100);
        var error = Error.NotFound(code: "Test", description: longDescription);

        // Act
        var str = error.ToString();

        // Assert
        str.Should().Contain("...");
        str.Length.Should().BeLessThan(200);
    }
}
