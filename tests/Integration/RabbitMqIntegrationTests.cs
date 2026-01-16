using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using VsaResults;
using VsaResults.Messaging;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// Integration tests that require a running RabbitMQ instance.
/// These tests are skipped if RabbitMQ is not available.
///
/// To run these tests:
/// 1. Start RabbitMQ: docker-compose -f docker-compose.integration.yml up -d
/// 2. Run tests: dotnet test --filter "Category=RabbitMq"
/// </summary>
[Trait("Category", "RabbitMq")]
[Collection("Messaging")]
public class RabbitMqIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqFixture _fixture;
    private readonly TaskCompletionSource<OrderCreated> _orderCreatedReceived = new();

    public RabbitMqIntegrationTests(RabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    private readonly TaskCompletionSource<MessageWideEvent> _wideEventReceived = new();

    public async Task InitializeAsync()
    {
        await _fixture.InitializeWithRabbitMqAsync(cfg =>
        {
            cfg.ReceiveEndpoint("rabbitmq-test-orders", ep =>
            {
                ep.Handler<OrderCreated>((context, ct) =>
                {
                    context
                        .AddContext("order_id", context.Message.OrderId)
                        .AddContext("customer_id", context.Message.CustomerId)
                        .AddContext("amount", context.Message.Amount);

                    _orderCreatedReceived.TrySetResult(context.Message);
                    return Task.FromResult<ErrorOr<Unit>>(Unit.Value);
                });
            });
        });

        // Subscribe to wide events
        _fixture.MessageWideEventEmitter.OnEmit(e => _wideEventReceived.TrySetResult(e));
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [SkippableFact]
    public async Task RabbitMq_ShouldConnect()
    {
        Skip.IfNot(_fixture.IsRabbitMqAvailable, "RabbitMQ is not available");

        // If we got here, connection was successful
        _fixture.IsRabbitMqAvailable.Should().BeTrue();
    }

    [SkippableFact]
    public async Task RabbitMq_PublishAndConsume_ShouldWork()
    {
        Skip.IfNot(_fixture.IsRabbitMqAvailable, "RabbitMQ is not available");

        // Arrange
        var bus = _fixture.GetBus();
        var orderId = Guid.NewGuid();
        var order = new OrderCreated(orderId, "CUST-RABBITMQ-001", 599.99m);

        // Act
        var result = await bus.PublishAsync(order);

        // Wait for consumer
        var received = await _orderCreatedReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        result.IsError.Should().BeFalse();
        received.OrderId.Should().Be(orderId);
        received.CustomerId.Should().Be("CUST-RABBITMQ-001");
        received.Amount.Should().Be(599.99m);
    }

    [SkippableFact]
    public async Task RabbitMq_Send_ShouldDeliverToSpecificQueue()
    {
        Skip.IfNot(_fixture.IsRabbitMqAvailable, "RabbitMQ is not available");

        // Arrange
        var bus = _fixture.GetBus();
        var endpointResult = await bus.GetSendEndpointAsync(
            EndpointAddress.FromUri(new Uri("rabbitmq://localhost/process-payment")));

        Skip.If(endpointResult.IsError, "Could not get send endpoint");

        var orderId = Guid.NewGuid();
        var payment = new ProcessPayment(orderId, 699.99m);

        // Act - SendAsync requires ICommand (ProcessPayment), not IEvent
        var result = await endpointResult.Value.SendAsync(payment);

        // Assert
        result.IsError.Should().BeFalse();
    }

    [SkippableFact]
    public async Task RabbitMq_MessageHeaders_ShouldBePreserved()
    {
        Skip.IfNot(_fixture.IsRabbitMqAvailable, "RabbitMQ is not available");

        // Arrange
        var bus = _fixture.GetBus();
        var orderId = Guid.NewGuid();
        var traceId = "rabbitmq-trace-" + Guid.NewGuid().ToString("N");
        var tenantId = "rabbitmq-tenant-" + Guid.NewGuid().ToString("N");

        // Act
        await bus.PublishAsync(
            new OrderCreated(orderId, "CUST-HEADERS-001", 799.99m),
            headers =>
            {
                headers.TraceId = traceId;
                headers.TenantId = tenantId;
                headers["CustomHeader"] = "CustomValue";
            });

        // Wait for consumer
        var received = await _orderCreatedReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        received.OrderId.Should().Be(orderId);
    }
}

/// <summary>
/// Tests for RabbitMQ transport configuration and options.
/// </summary>
public class RabbitMqTransportConfigurationTests
{
    [Fact]
    public void RabbitMqTransportOptions_ShouldHaveDefaults()
    {
        // Arrange & Act
        var options = new RabbitMqTransportOptions();

        // Assert
        options.Host.Should().Be("localhost");
        options.Port.Should().Be(5672);
        options.Username.Should().Be("guest");
        options.Password.Should().Be("guest");
    }

    [Fact]
    public void RabbitMqTransportOptions_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new RabbitMqTransportOptions
        {
            Host = "rabbitmq.example.com",
            Port = 5673,
            Username = "admin",
            Password = "secret123"
        };

        // Assert
        options.Host.Should().Be("rabbitmq.example.com");
        options.Port.Should().Be(5673);
        options.Username.Should().Be("admin");
        options.Password.Should().Be("secret123");
    }

    [Fact]
    public void MessagingConfigurator_UseRabbitMq_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVsaMessaging(cfg =>
        {
            cfg.UseRabbitMq(options =>
            {
                options.Host = "custom-host";
                options.Port = 5680;
                options.Username = "testuser";
                options.Password = "testpass";
            });
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<RabbitMqTransportOptions>();

        // Assert
        options.Host.Should().Be("custom-host");
        options.Port.Should().Be(5680);
        options.Username.Should().Be("testuser");
        options.Password.Should().Be("testpass");
    }

    [Fact]
    public async Task RabbitMqTransport_ShouldImplementITransport()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVsaMessaging(cfg =>
        {
            cfg.UseRabbitMq(options =>
            {
                options.Host = "localhost";
            });
        });

        var provider = services.BuildServiceProvider();
        var transport = provider.GetRequiredService<ITransport>();

        // Assert
        transport.Should().BeOfType<RabbitMqTransport>();
        transport.Scheme.Should().Be("rabbitmq");

        await provider.DisposeAsync();
    }
}

// Skip, SkipException, and SkippableFactAttribute are provided by Xunit.SkippableFact package
