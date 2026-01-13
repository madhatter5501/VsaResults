using FluentAssertions;
using VsaResults.Messaging;
using Xunit;

namespace Tests.Messaging;

public class CorrelationIdTests
{
    [Fact]
    public void New_ShouldCreateUniqueId()
    {
        // Act
        var id1 = CorrelationId.New();
        var id2 = CorrelationId.New();

        // Assert
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void From_ShouldPreserveValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = CorrelationId.From(guid);

        // Assert
        id.ToString().Should().Be(guid.ToString());
    }
}
