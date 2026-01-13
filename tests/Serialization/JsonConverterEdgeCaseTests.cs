using System.Text.Json;
using FluentAssertions;
using VsaResults;
using VsaResults.Serialization;

namespace Tests.Serialization;

public class JsonConverterEdgeCaseTests
{
    [Theory]
    [InlineData("isError")]
    [InlineData("IsError")]
    [InlineData("ISERROR")]
    [InlineData("isERROR")]
    public void ErrorOr_ShouldDeserialize_WithDifferentCasingOfIsError(string propertyName)
    {
        // Arrange
        var json = $$"""{"{{propertyName}}":false,"value":42}""";
        var options = CreateOptions();

        // Act
        var result = JsonSerializer.Deserialize<ErrorOr<int>>(json, options);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Theory]
    [InlineData("value")]
    [InlineData("Value")]
    [InlineData("VALUE")]
    public void ErrorOr_ShouldDeserialize_WithDifferentCasingOfValue(string propertyName)
    {
        // Arrange
        var json = $$"""{"isError":false,"{{propertyName}}":"test"}""";
        var options = CreateOptions();

        // Act
        var result = JsonSerializer.Deserialize<ErrorOr<string>>(json, options);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("test");
    }

    [Theory]
    [InlineData("errors")]
    [InlineData("Errors")]
    [InlineData("ERRORS")]
    public void ErrorOr_ShouldDeserialize_WithDifferentCasingOfErrors(string propertyName)
    {
        // Arrange
        var json = $$"""{"isError":true,"{{propertyName}}":[{"code":"Test.Error","description":"Test","type":0,"numericType":0}]}""";
        var options = CreateOptions();

        // Act
        var result = JsonSerializer.Deserialize<ErrorOr<int>>(json, options);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Test.Error");
    }

    [Fact]
    public void ErrorOr_ShouldInferValueState_WhenIsErrorMissing()
    {
        // Arrange
        var json = """{"value":123}""";
        var options = CreateOptions();

        // Act
        var result = JsonSerializer.Deserialize<ErrorOr<int>>(json, options);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(123);
    }

    [Fact]
    public void ErrorOr_ShouldInferErrorState_WhenIsErrorMissing()
    {
        // Arrange
        var json = """{"errors":[{"code":"Test.Error","description":"Test","type":0,"numericType":0}]}""";
        var options = CreateOptions();

        // Act
        var result = JsonSerializer.Deserialize<ErrorOr<int>>(json, options);

        // Assert
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void ErrorOr_ShouldHandleNullValueProperty()
    {
        // Arrange
        var json = """{"isError":true,"value":null,"errors":[{"code":"Test.Error","description":"Test","type":0,"numericType":0}]}""";
        var options = CreateOptions();

        // Act
        var result = JsonSerializer.Deserialize<ErrorOr<string>>(json, options);

        // Assert
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void ErrorOr_ShouldHandleNullErrorsProperty()
    {
        // Arrange
        var json = """{"isError":false,"value":"test","errors":null}""";
        var options = CreateOptions();

        // Act
        var result = JsonSerializer.Deserialize<ErrorOr<string>>(json, options);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("test");
    }

    [Fact]
    public void ErrorOr_ShouldIgnoreUnknownProperties()
    {
        // Arrange
        var json = """{"isError":false,"value":42,"unknownProperty":"ignored","anotherUnknown":123}""";
        var options = CreateOptions();

        // Act
        var result = JsonSerializer.Deserialize<ErrorOr<int>>(json, options);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ErrorOr_ShouldSerializeAndDeserializeNestedObject()
    {
        // Arrange
        ErrorOr<NestedObject> errorOr = new NestedObject
        {
            Name = "Parent",
            Child = new ChildObject { Value = 100 },
        };
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(errorOr, options);
        var result = JsonSerializer.Deserialize<ErrorOr<NestedObject>>(json, options);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Parent");
        result.Value.Child.Value.Should().Be(100);
    }

    [Fact]
    public void ErrorOr_ShouldSerializeAndDeserializeListValue()
    {
        // Arrange
        ErrorOr<List<string>> errorOr = new List<string> { "one", "two", "three" };
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(errorOr, options);
        var result = JsonSerializer.Deserialize<ErrorOr<List<string>>>(json, options);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
        result.Value.Should().ContainInOrder("one", "two", "three");
    }

    [Fact]
    public void ErrorOr_ShouldThrowOnInvalidJsonStructure()
    {
        // Arrange
        var json = """[]""";
        var options = CreateOptions();

        // Act
        var act = () => JsonSerializer.Deserialize<ErrorOr<int>>(json, options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void ErrorOr_ShouldThrowWhenNeitherValueNorErrors()
    {
        // Arrange
        var json = """{}""";
        var options = CreateOptions();

        // Act
        var act = () => JsonSerializer.Deserialize<ErrorOr<int>>(json, options);

        // Assert
        act.Should().Throw<JsonException>()
            .WithMessage("*neither valid value nor errors*");
    }

    [Fact]
    public void ErrorOr_ShouldSerializeAndDeserializeErrorWithComplexMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "userId", 123 },
            { "timestamp", "2024-01-01T00:00:00Z" },
            { "tags", new[] { "tag1", "tag2" } },
        };
        ErrorOr<int> errorOr = Error.NotFound("User.NotFound", "User not found", metadata);
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(errorOr, options);
        var result = JsonSerializer.Deserialize<ErrorOr<int>>(json, options);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Metadata.Should().NotBeNull();
        result.FirstError.Metadata!["userId"].Should().NotBeNull();
        result.FirstError.Metadata!["timestamp"].Should().NotBeNull();
    }

    [Theory]
    [InlineData(ErrorType.Failure)]
    [InlineData(ErrorType.Unexpected)]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    [InlineData(ErrorType.BadRequest)]
    [InlineData(ErrorType.Timeout)]
    [InlineData(ErrorType.Gone)]
    [InlineData(ErrorType.Locked)]
    [InlineData(ErrorType.TooManyRequests)]
    [InlineData(ErrorType.Unavailable)]
    public void ErrorOr_ShouldRoundtripAllErrorTypes(ErrorType errorType)
    {
        // Arrange
        var error = Error.Custom((int)errorType, $"{errorType}.Code", $"{errorType} description");
        ErrorOr<string> errorOr = error;
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(errorOr, options);
        var result = JsonSerializer.Deserialize<ErrorOr<string>>(json, options);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(errorType);
        result.FirstError.NumericType.Should().Be((int)errorType);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ErrorJsonConverter());
        options.Converters.Add(new ErrorOrJsonConverterFactory());
        return options;
    }

    private class NestedObject
    {
        public string Name { get; set; } = string.Empty;

        public ChildObject Child { get; set; } = new();
    }

    private class ChildObject
    {
        public int Value { get; set; }
    }
}
