using FluentAssertions;
using VsaResults.Messaging;
using Xunit;

namespace Tests.Messaging;

public class MessageEnvelopeTests
{
    public record OrderPlaced(Guid OrderId, decimal Amount) : IEvent;

    private readonly JsonMessageSerializer _serializer = new();
    private readonly MessageTypeResolver _typeResolver = new();

    [Fact]
    public void Create_ShouldSetMessageId()
    {
        // Arrange
        var message = new OrderPlaced(Guid.NewGuid(), 100m);

        // Act
        var envelope = CreateEnvelope(message);

        // Assert
        envelope.MessageId.Should().NotBe(default(MessageId));
    }

    [Fact]
    public void Create_ShouldSetSentTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var message = new OrderPlaced(Guid.NewGuid(), 100m);

        // Act
        var envelope = CreateEnvelope(message);

        // Assert
        envelope.SentTime.Should().BeOnOrAfter(before);
        envelope.SentTime.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_ShouldSetMessageTypes()
    {
        // Arrange
        var message = new OrderPlaced(Guid.NewGuid(), 100m);

        // Act
        var envelope = CreateEnvelope(message);

        // Assert
        envelope.MessageTypes.Should().NotBeEmpty();
        envelope.MessageTypes.Should().Contain(t => t.Contains("OrderPlaced"));
    }

    [Fact]
    public void Create_ShouldSerializeBody()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var message = new OrderPlaced(orderId, 150.50m);

        // Act
        var envelope = CreateEnvelope(message);

        // Assert
        envelope.Body.Should().NotBeEmpty();

        // Verify round-trip
        var result = _serializer.Deserialize<OrderPlaced>(envelope.Body);
        result.IsError.Should().BeFalse();
        result.Value.OrderId.Should().Be(orderId);
        result.Value.Amount.Should().Be(150.50m);
    }

    [Fact]
    public void Create_WithCorrelationId_ShouldPreserve()
    {
        // Arrange
        var message = new OrderPlaced(Guid.NewGuid(), 100m);
        var correlationId = CorrelationId.New();

        // Act
        var envelope = CreateEnvelope(message, correlationId);

        // Assert
        envelope.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void Create_WithoutCorrelationId_ShouldGenerateNew()
    {
        // Arrange
        var message = new OrderPlaced(Guid.NewGuid(), 100m);

        // Act
        var envelope = CreateEnvelope(message);

        // Assert
        envelope.CorrelationId.Should().NotBe(default(CorrelationId));
    }

    [Fact]
    public void CreateFollowUp_ShouldPreserveCorrelation()
    {
        // Arrange
        var originalMessage = new OrderPlaced(Guid.NewGuid(), 100m);
        var originalEnvelope = CreateEnvelope(originalMessage);

        var followUpMessage = new OrderPlaced(Guid.NewGuid(), 200m);
        var followUpMessageTypes = _typeResolver.GetMessageTypes<OrderPlaced>().ToList();
        var followUpBodyResult = _serializer.Serialize(followUpMessage);
        followUpBodyResult.IsError.Should().BeFalse();

        // Act
        var followUp = originalEnvelope.CreateFollowUp<OrderPlaced>(
            followUpMessageTypes,
            followUpBodyResult.Value);

        // Assert
        followUp.CorrelationId.Should().Be(originalEnvelope.CorrelationId);
        followUp.InitiatorId.Should().Be(originalEnvelope.MessageId);
        followUp.MessageId.Should().NotBe(originalEnvelope.MessageId);
    }

    private MessageEnvelope CreateEnvelope<T>(T message, CorrelationId? correlationId = null)
        where T : class, IMessage
    {
        var messageTypes = _typeResolver.GetMessageTypes<T>().ToList();
        var bodyResult = _serializer.Serialize(message);
        bodyResult.IsError.Should().BeFalse();

        return MessageEnvelope.Create(message, messageTypes, bodyResult.Value, correlationId);
    }
}
