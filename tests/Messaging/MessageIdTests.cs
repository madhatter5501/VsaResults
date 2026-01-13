using FluentAssertions;
using VsaResults.Messaging;
using Xunit;

namespace Tests.Messaging;

public class MessageIdTests
{
    [Fact]
    public void New_ShouldCreateUniqueId()
    {
        // Act
        var id1 = MessageId.New();
        var id2 = MessageId.New();

        // Assert
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void From_ShouldPreserveValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = MessageId.From(guid);

        // Assert
        id.ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Parse_ValidGuid_ShouldSucceed()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = MessageId.Parse(guid.ToString());

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void Parse_InvalidString_ShouldReturnError()
    {
        // Act
        var result = MessageId.Parse("not-a-guid");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(MessagingErrors.Codes.InvalidMessageId);
    }
}
