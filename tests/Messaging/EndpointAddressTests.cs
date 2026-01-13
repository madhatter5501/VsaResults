using FluentAssertions;
using VsaResults.Messaging;
using Xunit;

namespace Tests.Messaging;

public class EndpointAddressTests
{
    [Fact]
    public void InMemory_ShouldCreateCorrectUri()
    {
        // Act
        var address = EndpointAddress.InMemory("my-queue");

        // Assert
        address.Scheme.Should().Be("inmemory");
        address.Name.Should().Be("my-queue");
        address.Host.Should().Be("localhost");
    }

    [Fact]
    public void RabbitMq_ShouldCreateCorrectUri()
    {
        // Act
        var address = EndpointAddress.RabbitMq("myhost", "my-queue");

        // Assert
        address.Scheme.Should().Be("rabbitmq");
        address.Name.Should().Be("my-queue");
        address.Host.Should().Be("myhost");
    }

    [Fact]
    public void RabbitMq_WithPort_ShouldCreateCorrectUri()
    {
        // Act
        var address = EndpointAddress.RabbitMq("myhost", 5673, "my-queue");

        // Assert
        address.Scheme.Should().Be("rabbitmq");
        address.Port.Should().Be(5673);
        address.Name.Should().Be("my-queue");
    }

    [Fact]
    public void Parse_ValidUri_ShouldSucceed()
    {
        // Act
        var result = EndpointAddress.Parse("rabbitmq://localhost/orders");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Scheme.Should().Be("rabbitmq");
        result.Value.Name.Should().Be("orders");
    }

    [Fact]
    public void Parse_InvalidUri_ShouldReturnError()
    {
        // Act
        var result = EndpointAddress.Parse("not a valid uri");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(MessagingErrors.Codes.InvalidEndpointAddress);
    }

    [Fact]
    public void FromUri_ShouldWrapUri()
    {
        // Arrange
        var uri = new Uri("inmemory://localhost/test");

        // Act
        var address = EndpointAddress.FromUri(uri);

        // Assert
        address.Uri.Should().Be(uri);
    }
}
