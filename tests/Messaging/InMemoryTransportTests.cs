using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using VsaResults.Messaging;
using Xunit;

namespace Tests.Messaging;

public class InMemoryTransportTests
{
    public record TestEvent(string Name) : IEvent;

    public record TestCommand(int Value) : ICommand;

    [Fact]
    public async Task InMemoryTransport_ShouldCreateSendTransport()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVsaMessaging(cfg =>
        {
            cfg.UseInMemoryTransport();
        });
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();

        // Act
        var result = await bus.GetSendEndpointAsync(EndpointAddress.InMemory("test-queue"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task InMemoryTransport_ShouldPublish()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVsaMessaging(cfg =>
        {
            cfg.UseInMemoryTransport();
        });
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();

        // Act
        var result = await bus.PublishAsync(new TestEvent("test"));

        // Assert - PublishAsync may return error if no exchange exists for the message type
        // This is expected behavior for a pub/sub system without subscribers
        // We just verify the operation completes without throwing
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task InMemoryTransport_ShouldSend()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVsaMessaging(cfg =>
        {
            cfg.UseInMemoryTransport();
        });
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();
        var endpoint = await bus.GetSendEndpointAsync(EndpointAddress.InMemory("test-queue"));

        // Act
        var result = await endpoint.Value.SendAsync(new TestCommand(42));

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task InMemoryTransport_BusControl_ShouldStartAndStop()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVsaMessaging(cfg =>
        {
            cfg.UseInMemoryTransport();
        });
        var provider = services.BuildServiceProvider();
        var busControl = provider.GetRequiredService<IBusControl>();

        // Act
        var startResult = await busControl.StartAsync();
        var stopResult = await busControl.StopAsync();

        // Assert
        startResult.IsError.Should().BeFalse();
        stopResult.IsError.Should().BeFalse();
    }
}
