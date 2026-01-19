using FluentAssertions;
using VsaResults;

namespace Tests;

public class ValidationContextTests
{
    [Fact]
    public void Validate_WhenNoErrorsAdded_ShouldReturnSuccess()
    {
        // Arrange
        var request = new TestRequest("value", 5);

        // Act
        var result = ValidationExtensions.Validate()
            .RequiredString(request.Name, "Test.NameRequired", "Name is required")
            .RequiredValue(request.Count, "Test.CountRequired", "Count is required")
            .ToResult(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(request);
    }

    [Fact]
    public void Validate_WhenSingleErrorAdded_ShouldReturnError()
    {
        // Arrange
        var request = new TestRequest(string.Empty, 5);

        // Act
        var result = ValidationExtensions.Validate()
            .RequiredString(request.Name, "Test.NameRequired", "Name is required")
            .ToResult(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.FirstError.Code.Should().Be("Test.NameRequired");
    }

    [Fact]
    public void Validate_WhenMultipleErrorsAdded_ShouldReturnAllErrors()
    {
        // Arrange
        var request = new TestRequest(string.Empty, 0);

        // Act
        var result = ValidationExtensions.Validate()
            .RequiredString(request.Name, "Test.NameRequired", "Name is required")
            .RequiredValue(request.Count, "Test.CountRequired", "Count is required")
            .ToResult(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.Code == "Test.NameRequired");
        result.Errors.Should().Contain(e => e.Code == "Test.CountRequired");
    }

    [Fact]
    public void Must_WhenConditionIsTrue_ShouldNotAddError()
    {
        // Arrange
        var request = new TestRequest("value", 5);

        // Act
        var result = ValidationExtensions.Validate()
            .Must(request.Count > 0, "Test.CountPositive", "Count must be positive")
            .ToResult(request);

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void Must_WhenConditionIsFalse_ShouldAddError()
    {
        // Arrange
        var request = new TestRequest("value", -1);

        // Act
        var result = ValidationExtensions.Validate()
            .Must(request.Count > 0, "Test.CountPositive", "Count must be positive")
            .ToResult(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Test.CountPositive");
    }

    [Fact]
    public void RequiredCollection_WhenCollectionIsNull_ShouldAddError()
    {
        // Act
        var result = ValidationExtensions.Validate()
            .RequiredCollection<string>(null, "Test.ItemsRequired", "Items are required")
            .ToResult("test");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Test.ItemsRequired");
    }

    [Fact]
    public void RequiredCollection_WhenCollectionIsEmpty_ShouldAddError()
    {
        // Act
        var result = ValidationExtensions.Validate()
            .RequiredCollection(Array.Empty<string>(), "Test.ItemsRequired", "Items are required")
            .ToResult("test");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Test.ItemsRequired");
    }

    [Fact]
    public void RequiredCollection_WhenCollectionHasItems_ShouldNotAddError()
    {
        // Act
        var result = ValidationExtensions.Validate()
            .RequiredCollection(new[] { "item1" }, "Test.ItemsRequired", "Items are required")
            .ToResult("test");

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void MaxLength_WhenValueExceedsMax_ShouldAddError()
    {
        // Act
        var result = ValidationExtensions.Validate()
            .MaxLength("toolongvalue", 5, "Test.TooLong", "Value is too long")
            .ToResult("test");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Test.TooLong");
    }

    [Fact]
    public void MaxLength_WhenValueIsWithinMax_ShouldNotAddError()
    {
        // Act
        var result = ValidationExtensions.Validate()
            .MaxLength("ok", 5, "Test.TooLong", "Value is too long")
            .ToResult("test");

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void InRange_WhenValueIsInRange_ShouldNotAddError()
    {
        // Act
        var result = ValidationExtensions.Validate()
            .InRange(5, 1, 10, "Test.OutOfRange", "Value is out of range")
            .ToResult("test");

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void InRange_WhenValueIsBelowMin_ShouldAddError()
    {
        // Act
        var result = ValidationExtensions.Validate()
            .InRange(0, 1, 10, "Test.OutOfRange", "Value is out of range")
            .ToResult("test");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Test.OutOfRange");
    }

    [Fact]
    public void InRange_WhenValueIsAboveMax_ShouldAddError()
    {
        // Act
        var result = ValidationExtensions.Validate()
            .InRange(11, 1, 10, "Test.OutOfRange", "Value is out of range")
            .ToResult("test");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Test.OutOfRange");
    }

    [Fact]
    public void ValidEmail_WhenEmailIsValid_ShouldNotAddError()
    {
        // Act
        var result = ValidationExtensions.Validate()
            .ValidEmail("user@example.com", "Test.InvalidEmail", "Invalid email")
            .ToResult("test");

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void ValidEmail_WhenEmailIsInvalid_ShouldAddError()
    {
        // Act
        var result = ValidationExtensions.Validate()
            .ValidEmail("notanemail", "Test.InvalidEmail", "Invalid email")
            .ToResult("test");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Test.InvalidEmail");
    }

    [Fact]
    public void ValidEmail_WhenEmailIsNull_ShouldNotAddError()
    {
        // Act (null email skips validation per design - use RequiredString for null checks)
        var result = ValidationExtensions.Validate()
            .ValidEmail(null, "Test.InvalidEmail", "Invalid email")
            .ToResult("test");

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void When_WhenConditionIsTrue_ShouldRunValidation()
    {
        // Arrange
        var shouldValidate = true;

        // Act
        var result = ValidationExtensions.Validate()
            .When(shouldValidate, ctx => ctx.Must(false, "Test.Failed", "This should fail"))
            .ToResult("test");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Test.Failed");
    }

    [Fact]
    public void When_WhenConditionIsFalse_ShouldSkipValidation()
    {
        // Arrange
        var shouldValidate = false;

        // Act
        var result = ValidationExtensions.Validate()
            .When(shouldValidate, ctx => ctx.Must(false, "Test.Failed", "This should fail"))
            .ToResult("test");

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void AddErrorIf_WhenConditionIsTrue_ShouldAddError()
    {
        // Arrange
        var ctx = new ValidationContext();

        // Act
        ctx.AddErrorIf(true, Error.Validation("Test.Error", "Test error"));

        // Assert
        ctx.HasErrors.Should().BeTrue();
        ctx.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void AddErrorIf_WhenConditionIsFalse_ShouldNotAddError()
    {
        // Arrange
        var ctx = new ValidationContext();

        // Act
        ctx.AddErrorIf(false, Error.Validation("Test.Error", "Test error"));

        // Assert
        ctx.HasErrors.Should().BeFalse();
        ctx.Errors.Should().BeEmpty();
    }

    private sealed record TestRequest(string Name, int Count);
}
