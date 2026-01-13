using Xunit;

namespace Tests.Integration;

[CollectionDefinition("Messaging")]
public sealed class MessagingCollection : ICollectionFixture<RabbitMqFixture>
{
}
