using Microsoft.Extensions.DependencyInjection;
using VsaResults;
using VsaResults.Messaging;
using VsaResults.Messaging.RabbitMq;

namespace Tests.Integration;

/// <summary>
/// Fixture for RabbitMQ integration tests.
/// Manages RabbitMQ connection and provides helper methods for testing.
/// </summary>
public sealed class RabbitMqFixture : IAsyncDisposable
{
    private ServiceProvider? _serviceProvider;
    private IBusControl? _busControl;

    /// <summary>
    /// Gets the RabbitMQ host (from environment or default).
    /// </summary>
    public string Host => Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";

    /// <summary>
    /// Gets the RabbitMQ port.
    /// </summary>
    public int Port => int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");

    /// <summary>
    /// Gets the RabbitMQ username.
    /// </summary>
    public string Username => Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest";

    /// <summary>
    /// Gets the RabbitMQ password.
    /// </summary>
    public string Password => Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";

    /// <summary>
    /// Gets a value indicating whether RabbitMQ is available.
    /// </summary>
    public bool IsRabbitMqAvailable { get; private set; }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public IServiceProvider ServiceProvider => _serviceProvider
        ?? throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Gets the test wide event emitter.
    /// </summary>
    public TestWideEventEmitter WideEventEmitter { get; } = new();

    /// <summary>
    /// Gets the test message wide event emitter.
    /// </summary>
    public TestMessageWideEventEmitter MessageWideEventEmitter { get; } = new();

    /// <summary>
    /// Initializes the fixture with RabbitMQ transport.
    /// </summary>
    public async Task InitializeWithRabbitMqAsync(
        Action<IMessagingConfigurator>? configure = null)
    {
        await ResetAsync();
        var services = new ServiceCollection();

        services.AddSingleton<IWideEventEmitter>(WideEventEmitter);
        services.AddSingleton<IMessageWideEventEmitter>(MessageWideEventEmitter);

        services.AddVsaMessaging(cfg =>
        {
            cfg.UseRabbitMq(options =>
            {
                options.Host = Host;
                options.Port = Port;
                options.Username = Username;
                options.Password = Password;
            });

            configure?.Invoke(cfg);
        });

        _serviceProvider = services.BuildServiceProvider();
        _busControl = _serviceProvider.GetRequiredService<IBusControl>();

        try
        {
            var result = await _busControl.StartAsync();
            IsRabbitMqAvailable = !result.IsError;
        }
        catch
        {
            IsRabbitMqAvailable = false;
        }
    }

    /// <summary>
    /// Initializes the fixture with InMemory transport (for tests that don't need RabbitMQ).
    /// </summary>
    public async Task InitializeWithInMemoryAsync(
        Action<IMessagingConfigurator>? configure = null)
    {
        await ResetAsync();
        var services = new ServiceCollection();

        services.AddSingleton<IWideEventEmitter>(WideEventEmitter);
        services.AddSingleton<IMessageWideEventEmitter>(MessageWideEventEmitter);

        services.AddVsaMessaging(cfg =>
        {
            cfg.UseInMemoryTransport();
            configure?.Invoke(cfg);
        });

        _serviceProvider = services.BuildServiceProvider();
        _busControl = _serviceProvider.GetRequiredService<IBusControl>();

        await _busControl.StartAsync();
        IsRabbitMqAvailable = true; // InMemory is always available
    }

    /// <summary>
    /// Gets the bus for sending and publishing messages.
    /// </summary>
    public IBus GetBus() => ServiceProvider.GetRequiredService<IBus>();

    /// <summary>
    /// Gets the send endpoint for a specific address.
    /// </summary>
    public async Task<ISendEndpoint> GetSendEndpointAsync(string queueName)
    {
        var bus = GetBus();
        var result = await bus.GetSendEndpointAsync(EndpointAddress.InMemory(queueName));
        return result.Value;
    }

    /// <summary>
    /// Clears all captured events.
    /// </summary>
    public void ClearEvents()
    {
        WideEventEmitter.Clear();
        MessageWideEventEmitter.Clear();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await ResetAsync();
    }

    private async Task ResetAsync()
    {
        if (_busControl is not null)
        {
            await _busControl.StopAsync();
            _busControl = null;
        }

        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
            _serviceProvider = null;
        }

        IsRabbitMqAvailable = false;
        ClearEvents();
    }
}

/// <summary>
/// Base class for integration tests that require messaging infrastructure.
/// </summary>
public abstract class MessagingIntegrationTestBase : IAsyncLifetime
{
    protected MessagingIntegrationTestBase(RabbitMqFixture fixture)
    {
        Fixture = fixture;
    }

    protected RabbitMqFixture Fixture { get; }

    /// <summary>
    /// Override to configure the messaging system.
    /// </summary>
    protected virtual Action<IMessagingConfigurator>? ConfigureMessaging => null;

    /// <summary>
    /// Override to use RabbitMQ instead of InMemory transport.
    /// </summary>
    protected virtual bool UseRabbitMq => false;

    public virtual async Task InitializeAsync()
    {
        if (UseRabbitMq)
        {
            await Fixture.InitializeWithRabbitMqAsync(ConfigureMessaging);
        }
        else
        {
            await Fixture.InitializeWithInMemoryAsync(ConfigureMessaging);
        }
    }

    public virtual async Task DisposeAsync()
    {
        await Fixture.DisposeAsync();
    }
}
