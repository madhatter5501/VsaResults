using FluentAssertions;
using VsaResults.Messaging;
using Xunit;

namespace Tests.Messaging;

public class MessageSerializerTests
{
    private readonly JsonMessageSerializer _serializer = new();

    public record TestMessage : IMessage
    {
        public required string Content { get; init; }
        public int Value { get; init; }
    }

    public record TestEvent(string Name, int Count) : IEvent;

    public record TestCommand(Guid OrderId, decimal Amount) : ICommand;

    [Fact]
    public void Serialize_SimpleMessage_ShouldRoundTrip()
    {
        // Arrange
        var message = new TestMessage { Content = "Hello", Value = 42 };

        // Act
        var serializeResult = _serializer.Serialize(message);
        serializeResult.IsError.Should().BeFalse();
        var result = _serializer.Deserialize<TestMessage>(serializeResult.Value);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Content.Should().Be("Hello");
        result.Value.Value.Should().Be(42);
    }

    [Fact]
    public void Serialize_Event_ShouldRoundTrip()
    {
        // Arrange
        var evt = new TestEvent("OrderCreated", 5);

        // Act
        var serializeResult = _serializer.Serialize(evt);
        serializeResult.IsError.Should().BeFalse();
        var result = _serializer.Deserialize<TestEvent>(serializeResult.Value);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("OrderCreated");
        result.Value.Count.Should().Be(5);
    }

    [Fact]
    public void Serialize_Command_ShouldRoundTrip()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new TestCommand(orderId, 99.99m);

        // Act
        var serializeResult = _serializer.Serialize(command);
        serializeResult.IsError.Should().BeFalse();
        var result = _serializer.Deserialize<TestCommand>(serializeResult.Value);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.OrderId.Should().Be(orderId);
        result.Value.Amount.Should().Be(99.99m);
    }

    [Fact]
    public void Deserialize_InvalidBytes_ShouldReturnError()
    {
        // Arrange
        var invalidBytes = "{ invalid json"u8.ToArray();

        // Act
        var result = _serializer.Deserialize<TestMessage>(invalidBytes);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(MessagingErrors.Codes.DeserializationFailed);
    }

    [Fact]
    public void Deserialize_EmptyBytes_ShouldReturnError()
    {
        // Arrange
        var emptyBytes = Array.Empty<byte>();

        // Act
        var result = _serializer.Deserialize<TestMessage>(emptyBytes);

        // Assert
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_ByType_ShouldWork()
    {
        // Arrange
        var message = new TestMessage { Content = "Test", Value = 100 };
        var serializeResult = _serializer.Serialize(message);
        serializeResult.IsError.Should().BeFalse();

        // Act
        var result = _serializer.Deserialize(serializeResult.Value, typeof(TestMessage));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeOfType<TestMessage>();
        ((TestMessage)result.Value!).Content.Should().Be("Test");
    }
}
