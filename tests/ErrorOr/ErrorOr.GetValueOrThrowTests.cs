using FluentAssertions;
using VsaResults;

namespace Tests;

public class GetValueOrThrowTests
{
    [Fact]
    public void GetValueOrThrow_WhenSuccess_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 42;

        // Act
        var value = errorOrInt.GetValueOrThrow();

        // Assert
        value.Should().Be(42);
    }

    [Fact]
    public void GetValueOrThrow_WhenError_ShouldThrowWithDefaultMessage()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Error.NotFound(code: "User.NotFound");

        // Act
        var act = () => errorOrInt.GetValueOrThrow();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*User.NotFound*")
            .WithMessage("*1 error(s)*");
    }

    [Fact]
    public void GetValueOrThrow_WhenError_ShouldThrowWithCustomMessage()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Error.NotFound();
        const string customMessage = "Custom error message";

        // Act
        var act = () => errorOrInt.GetValueOrThrow(customMessage);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(customMessage);
    }

    [Fact]
    public void GetValueOrThrow_WhenMultipleErrors_ShouldListAllErrorCodes()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new List<Error>
        {
            Error.Validation(code: "Field.A"),
            Error.Validation(code: "Field.B"),
            Error.Validation(code: "Field.C"),
        };

        // Act
        var act = () => errorOrInt.GetValueOrThrow();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Field.A*")
            .WithMessage("*Field.B*")
            .WithMessage("*Field.C*")
            .WithMessage("*3 error(s)*");
    }

    [Fact]
    public void GetValueOrThrow_WithReferenceType_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<string> errorOrString = "Hello World";

        // Act
        var value = errorOrString.GetValueOrThrow();

        // Assert
        value.Should().Be("Hello World");
    }

    [Fact]
    public void GetValueOrThrow_CanBeUsedInExpressionBodiedMethods()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 42;

        // Act - simulating usage pattern
        int GetValue() => errorOrInt.GetValueOrThrow("Value must be present");

        // Assert
        GetValue().Should().Be(42);
    }

    [Fact]
    public void GetValueOrThrow_CanBeUsedWithNullCoalescing()
    {
        // Arrange
        ErrorOr<string?> errorOrString = "Hello";

        // Act
        var value = errorOrString.GetValueOrThrow() ?? "Default";

        // Assert
        value.Should().Be("Hello");
    }
}
