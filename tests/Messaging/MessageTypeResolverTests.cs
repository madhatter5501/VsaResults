using FluentAssertions;
using VsaResults.Messaging;
using Xunit;

namespace Tests.Messaging;

public class MessageTypeResolverTests
{
    private readonly MessageTypeResolver _resolver = new();

    public record OrderCreatedEvent(Guid OrderId) : IEvent;

    public record CreateOrderCommand(string CustomerId) : ICommand;

    public record SimpleMessage(string Text) : IMessage;

    [Fact]
    public void GetPrimaryIdentifier_ShouldReturnUrn()
    {
        // Act
        var identifier = _resolver.GetPrimaryIdentifier<OrderCreatedEvent>();

        // Assert
        identifier.Should().StartWith("urn:message:");
        identifier.Should().Contain("OrderCreatedEvent");
    }

    [Fact]
    public void GetMessageTypes_ShouldIncludePrimaryType()
    {
        // Act
        var types = _resolver.GetMessageTypes<OrderCreatedEvent>().ToList();

        // Assert
        types.Should().NotBeEmpty();
        types.Should().Contain(t => t.Contains("OrderCreatedEvent"));
    }

    [Fact]
    public void GetPrimaryIdentifier_ByType_ShouldMatchGeneric()
    {
        // Act
        var genericIdentifier = _resolver.GetPrimaryIdentifier<CreateOrderCommand>();
        var typeIdentifier = _resolver.GetPrimaryIdentifier(typeof(CreateOrderCommand));

        // Assert
        genericIdentifier.Should().Be(typeIdentifier);
    }
}
