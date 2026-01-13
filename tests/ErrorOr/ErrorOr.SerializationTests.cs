using System.Text.Json;
using ErrorOr;
using ErrorOr.Serialization;
using FluentAssertions;

namespace Tests;

public class SerializationTests
{
    [Fact]
    public void Error_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var error = Error.NotFound("User.NotFound", "User was not found");
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(error, options);
        var deserialized = JsonSerializer.Deserialize<Error>(json, options);

        // Assert
        deserialized.Code.Should().Be(error.Code);
        deserialized.Description.Should().Be(error.Description);
        deserialized.Type.Should().Be(error.Type);
        deserialized.NumericType.Should().Be(error.NumericType);
    }

    [Fact]
    public void Error_WithMetadata_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var error = Error.NotFound(
            "User.NotFound",
            "User was not found",
            new Dictionary<string, object> { { "UserId", 123 } });
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(error, options);
        var deserialized = JsonSerializer.Deserialize<Error>(json, options);

        // Assert
        deserialized.Code.Should().Be(error.Code);
        deserialized.Metadata.Should().NotBeNull();
        deserialized.Metadata!["UserId"].Should().NotBeNull();
    }

    [Fact]
    public void ErrorOr_WhenValue_ShouldSerializeAndDeserialize()
    {
        // Arrange
        ErrorOr<int> errorOr = 42;
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(errorOr, options);
        var deserialized = JsonSerializer.Deserialize<ErrorOr<int>>(json, options);

        // Assert
        deserialized.IsError.Should().BeFalse();
        deserialized.Value.Should().Be(42);
    }

    [Fact]
    public void ErrorOr_WhenError_ShouldSerializeAndDeserialize()
    {
        // Arrange
        ErrorOr<int> errorOr = Error.NotFound("Item.NotFound", "Item was not found");
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(errorOr, options);
        var deserialized = JsonSerializer.Deserialize<ErrorOr<int>>(json, options);

        // Assert
        deserialized.IsError.Should().BeTrue();
        deserialized.Errors.Should().HaveCount(1);
        deserialized.FirstError.Code.Should().Be("Item.NotFound");
    }

    [Fact]
    public void ErrorOr_WhenMultipleErrors_ShouldSerializeAndDeserialize()
    {
        // Arrange
        ErrorOr<int> errorOr = new List<Error>
        {
            Error.NotFound("Item.NotFound", "Item was not found"),
            Error.Validation("Item.Invalid", "Item is invalid"),
        };
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(errorOr, options);
        var deserialized = JsonSerializer.Deserialize<ErrorOr<int>>(json, options);

        // Assert
        deserialized.IsError.Should().BeTrue();
        deserialized.Errors.Should().HaveCount(2);
        deserialized.Errors[0].Code.Should().Be("Item.NotFound");
        deserialized.Errors[1].Code.Should().Be("Item.Invalid");
    }

    [Fact]
    public void ErrorOr_WithComplexValue_ShouldSerializeAndDeserialize()
    {
        // Arrange
        ErrorOr<TestPerson> errorOr = new TestPerson { Name = "John", Age = 30 };
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(errorOr, options);
        var deserialized = JsonSerializer.Deserialize<ErrorOr<TestPerson>>(json, options);

        // Assert
        deserialized.IsError.Should().BeFalse();
        deserialized.Value.Name.Should().Be("John");
        deserialized.Value.Age.Should().Be(30);
    }

    [Fact]
    public void ErrorOr_Serialization_ShouldIncludeIsErrorFlag()
    {
        // Arrange
        ErrorOr<int> errorOr = 42;
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(errorOr, options);

        // Assert
        json.Should().Contain("\"isError\":false");
        json.Should().Contain("\"value\":42");
    }

    [Fact]
    public void ErrorOr_ErrorSerialization_ShouldIncludeIsErrorFlag()
    {
        // Arrange
        ErrorOr<int> errorOr = Error.NotFound();
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(errorOr, options);

        // Assert
        json.Should().Contain("\"isError\":true");
        json.Should().Contain("\"errors\":");
    }

    [Fact]
    public void AllNewErrorTypes_ShouldSerializeCorrectly()
    {
        // Arrange
        var options = CreateOptions();
        var errors = new[]
        {
            Error.BadRequest(),
            Error.Timeout(),
            Error.Gone(),
            Error.Locked(),
            Error.TooManyRequests(),
            Error.Unavailable(),
        };

        // Act & Assert
        foreach (var error in errors)
        {
            var json = JsonSerializer.Serialize(error, options);
            var deserialized = JsonSerializer.Deserialize<Error>(json, options);
            deserialized.Type.Should().Be(error.Type);
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ErrorJsonConverter());
        options.Converters.Add(new ErrorOrJsonConverterFactory());
        return options;
    }

    private class TestPerson
    {
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }
    }
}
