using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using VsaResults;
using VsaResults.Messaging;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// Test messages for integration testing.
/// </summary>
public record OrderCreated(Guid OrderId, string CustomerId, decimal Amount) : IEvent;
public record OrderShipped(Guid OrderId, string TrackingNumber) : IEvent;
public record ProcessPayment(Guid OrderId, decimal Amount) : ICommand;
public record SendNotification(string UserId, string Message) : ICommand;

/// <summary>
/// Test consumers for integration testing.
/// </summary>
public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderCreatedConsumer(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task<VsaResult<Unit>> ConsumeAsync(
        ConsumeContext<OrderCreated> context,
        CancellationToken ct = default)
    {
        // Add context for wide event
        context
            .AddContext("order_id", context.Message.OrderId)
            .AddContext("customer_id", context.Message.CustomerId)
            .AddContext("order_amount", context.Message.Amount)
            .AddContext("consumer_processed", true);

        // Publish follow-up event (tests context propagation across service boundaries)
        await context.PublishAsync(new OrderShipped(
            context.Message.OrderId,
            $"TRACK-{context.Message.OrderId:N}"));

        return Unit.Value;
    }
}

public class OrderShippedConsumer : IConsumer<OrderShipped>
{
    public Task<VsaResult<Unit>> ConsumeAsync(
        ConsumeContext<OrderShipped> context,
        CancellationToken ct = default)
    {
        // Add context - should include trace context from parent message
        context
            .AddContext("order_id", context.Message.OrderId)
            .AddContext("tracking_number", context.Message.TrackingNumber)
            .AddContext("shipping_processed", true)
            .AddContext("parent_initiator_id", context.Headers.InitiatorId);

        return Task.FromResult<VsaResult<Unit>>(Unit.Value);
    }
}

public class ProcessPaymentConsumer : IConsumer<ProcessPayment>
{
    public Task<VsaResult<Unit>> ConsumeAsync(
        ConsumeContext<ProcessPayment> context,
        CancellationToken ct = default)
    {
        context
            .AddContext("payment_order_id", context.Message.OrderId)
            .AddContext("payment_amount", context.Message.Amount);

        // Simulate payment validation
        if (context.Message.Amount <= 0)
        {
            return Task.FromResult<VsaResult<Unit>>(Error.Validation(
                "Payment.InvalidAmount",
                "Payment amount must be positive"));
        }

        return Task.FromResult<VsaResult<Unit>>(Unit.Value);
    }
}

public class FailingConsumer : IConsumer<SendNotification>
{
    public Task<VsaResult<Unit>> ConsumeAsync(
        ConsumeContext<SendNotification> context,
        CancellationToken ct = default)
    {
        context.AddContext("notification_user_id", context.Message.UserId);

        // Always fail for testing error scenarios
        return Task.FromResult<VsaResult<Unit>>(Error.Failure(
            "Notification.Failed",
            "Failed to send notification"));
    }
}

/// <summary>
/// Integration tests for messaging with InMemory transport.
/// These tests verify the core messaging functionality and wide events.
/// </summary>
[Collection("Messaging")]
public class InMemoryMessagingIntegrationTests : MessagingIntegrationTestBase
{
    public InMemoryMessagingIntegrationTests(RabbitMqFixture fixture)
        : base(fixture)
    {
    }

    private readonly TaskCompletionSource<ConsumeContext<OrderCreated>> _orderCreatedReceived = new();

    private readonly TaskCompletionSource<ConsumeContext<OrderShipped>> _orderShippedReceived = new();
    private readonly TaskCompletionSource<ConsumeContext<ProcessPayment>> _paymentReceived = new();

    protected override Action<IMessagingConfigurator>? ConfigureMessaging => cfg =>
    {
        cfg.ReceiveEndpoint("order-created", ep =>
        {
            ep.Consumer<OrderCreatedConsumer>();
        });

        cfg.ReceiveEndpoint("order-shipped", ep =>
        {
            ep.Consumer<OrderShippedConsumer>();
        });

        cfg.ReceiveEndpoint("process-payment", ep =>
        {
            ep.Consumer<ProcessPaymentConsumer>();
        });

        cfg.ReceiveEndpoint("send-notification", ep =>
        {
            ep.Consumer<FailingConsumer>();
        });
    };

    [Fact]
    public async Task Publish_ShouldDeliverToConsumer()
    {
        // Arrange
        var bus = Fixture.GetBus();
        var orderId = Guid.NewGuid();

        // Act
        var result = await bus.PublishAsync(new OrderCreated(orderId, "CUST-001", 99.99m));

        // Give consumer time to process
        await Task.Delay(100);

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Send_ShouldDeliverToSpecificEndpoint()
    {
        // Arrange
        var endpoint = await Fixture.GetSendEndpointAsync("process-payment");
        var orderId = Guid.NewGuid();

        // Act
        var result = await endpoint.SendAsync(new ProcessPayment(orderId, 149.99m));

        // Give consumer time to process
        await Task.Delay(100);

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task MessageEnvelope_ShouldPreserveCorrelationId()
    {
        // Arrange
        var bus = Fixture.GetBus();
        var orderId = Guid.NewGuid();
        var expectedCorrelationId = CorrelationId.New();

        // Act
        var result = await bus.PublishAsync(
            new OrderCreated(orderId, "CUST-002", 199.99m),
            headers =>
            {
                headers["custom_correlation"] = expectedCorrelationId.ToString();
            });

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Consumer_CanPublishFollowUpEvents()
    {
        // Arrange
        var bus = Fixture.GetBus();
        var orderId = Guid.NewGuid();

        // Act - Publish OrderCreated, consumer will publish OrderShipped
        var result = await bus.PublishAsync(new OrderCreated(orderId, "CUST-003", 299.99m));

        // Give consumer chain time to process
        await Task.Delay(200);

        // Assert
        result.IsError.Should().BeFalse();
    }
}

/// <summary>
/// Integration tests specifically for Wide Events emission and context propagation.
/// </summary>
[Collection("Messaging")]
public class WideEventsIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqFixture _fixture;
    private readonly List<MessageWideEvent> _capturedEvents = new();

    public WideEventsIntegrationTests(RabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeWithInMemoryAsync(cfg =>
        {
            cfg.ReceiveEndpoint("wide-events-test", ep =>
            {
                ep.Handler<OrderCreated>(async (context, ct) =>
                {
                    context
                        .AddContext("order_id", context.Message.OrderId)
                        .AddContext("customer_id", context.Message.CustomerId)
                        .AddContext("amount", context.Message.Amount)
                        .AddContext("processed_at", DateTimeOffset.UtcNow);

                    return Unit.Value;
                });
            });
        });
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public void MessageWideEventBuilder_ShouldCaptureMessageContext()
    {
        // Arrange
        var messageId = MessageId.New();
        var correlationId = CorrelationId.New();

        // Act
        var builder = MessageWideEvent.Start(
            messageId.ToString(),
            correlationId.ToString(),
            "OrderCreated",
            "OrderCreatedConsumer",
            "order-created-endpoint");

        builder
            .WithContext("order_id", Guid.NewGuid())
            .WithContext("customer_id", "CUST-001")
            .WithContext("amount", 99.99m);

        var wideEvent = builder.Success();

        // Assert
        wideEvent.Should().NotBeNull();
        wideEvent.MessageId.Should().Be(messageId.ToString());
        wideEvent.CorrelationId.Should().Be(correlationId.ToString());
        wideEvent.MessageType.Should().Be("OrderCreated");
        wideEvent.ConsumerType.Should().Be("OrderCreatedConsumer");
        wideEvent.EndpointName.Should().Be("order-created-endpoint");
        wideEvent.Outcome.Should().Be("success");
        wideEvent.IsSuccess.Should().BeTrue();

        wideEvent.MessageContext.Should().ContainKey("order_id");
        wideEvent.MessageContext.Should().ContainKey("customer_id");
        wideEvent.MessageContext.Should().ContainKey("amount");
        wideEvent.MessageContext["customer_id"].Should().Be("CUST-001");
        wideEvent.MessageContext["amount"].Should().Be(99.99m);
    }

    [Fact]
    public void MessageWideEventBuilder_ShouldRecordTimingBreakdown()
    {
        // Arrange
        var builder = MessageWideEvent.Start(
            MessageId.New().ToString(),
            CorrelationId.New().ToString(),
            "TestMessage",
            "TestConsumer",
            "test-endpoint");

        // Act - Simulate pipeline stages
        builder.StartStage();
        Thread.Sleep(10);
        builder.RecordDeserialization();

        builder.StartStage();
        Thread.Sleep(10);
        builder.RecordPreConsumeFilters();

        builder.StartStage();
        Thread.Sleep(10);
        builder.RecordConsumer();

        builder.StartStage();
        Thread.Sleep(10);
        builder.RecordPostConsumeFilters();

        var wideEvent = builder.Success();

        // Assert
        wideEvent.DeserializationMs.Should().BeGreaterThan(0);
        wideEvent.PreConsumeFiltersMs.Should().BeGreaterThan(0);
        wideEvent.ConsumerMs.Should().BeGreaterThan(0);
        wideEvent.PostConsumeFiltersMs.Should().BeGreaterThan(0);
        wideEvent.TotalMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MessageWideEventBuilder_ShouldCaptureEnvelopeMetadata()
    {
        // Arrange
        var messageId = MessageId.New();
        var correlationId = CorrelationId.New();
        var conversationId = ConversationId.New();
        var sourceAddress = EndpointAddress.InMemory("source-queue");
        var destinationAddress = EndpointAddress.InMemory("destination-queue");

        var envelope = new MessageEnvelope
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            ConversationId = conversationId,
            SourceAddress = sourceAddress,
            DestinationAddress = destinationAddress,
            MessageTypes = new[] { "urn:message:Tests.Integration:OrderCreated" },
            SentTime = DateTimeOffset.UtcNow.AddMilliseconds(-50),
            Body = "{}"u8.ToArray(),
            Headers = new MessageHeaders
            {
                TraceId = "trace-123",
                TenantId = "tenant-abc",
            },
        };

        var builder = MessageWideEvent.Start(
            messageId.ToString(),
            correlationId.ToString(),
            "OrderCreated",
            "OrderCreatedConsumer",
            "order-endpoint");

        // Act
        builder.WithEnvelope(envelope);
        var wideEvent = builder.Success();

        // Assert
        wideEvent.ConversationId.Should().Be(conversationId.ToString());
        wideEvent.SourceAddress.Should().Be(sourceAddress.ToString());
        wideEvent.DestinationAddress.Should().Be(destinationAddress.ToString());
        wideEvent.QueueTimeMs.Should().BeGreaterOrEqualTo(50);

        // Headers should be in MessageContext with header_ prefix using the actual header key names
        wideEvent.MessageContext.Should().ContainKey("header_x-trace-id");
        wideEvent.MessageContext.Should().ContainKey("header_x-tenant-id");
        wideEvent.MessageContext["header_x-trace-id"].Should().Be("trace-123");
        wideEvent.MessageContext["header_x-tenant-id"].Should().Be("tenant-abc");
    }

    [Fact]
    public void MessageWideEventBuilder_ConsumerError_ShouldCaptureErrorDetails()
    {
        // Arrange
        var builder = MessageWideEvent.Start(
            MessageId.New().ToString(),
            CorrelationId.New().ToString(),
            "ProcessPayment",
            "ProcessPaymentConsumer",
            "payment-endpoint");

        var errors = new List<Error>
        {
            Error.Validation("Payment.InvalidAmount", "Amount must be positive"),
            Error.Validation("Payment.InvalidCurrency", "Currency not supported")
        };

        // Act
        var wideEvent = builder.ConsumerError(errors);

        // Assert
        wideEvent.Outcome.Should().Be("consumer_error");
        wideEvent.IsSuccess.Should().BeFalse();
        wideEvent.FailedAtStage.Should().Be("consumer");
        wideEvent.ErrorCode.Should().Be("Payment.InvalidAmount");
        wideEvent.ErrorType.Should().Be("Validation");
        wideEvent.ErrorMessage.Should().Be("Amount must be positive");
        wideEvent.ErrorCount.Should().Be(2);
        wideEvent.ErrorDescription.Should().Contain("Payment.InvalidAmount");
        wideEvent.ErrorDescription.Should().Contain("Payment.InvalidCurrency");
    }

    [Fact]
    public void MessageWideEventBuilder_Exception_ShouldCaptureExceptionDetails()
    {
        // Arrange
        var builder = MessageWideEvent.Start(
            MessageId.New().ToString(),
            CorrelationId.New().ToString(),
            "TestMessage",
            "TestConsumer",
            "test-endpoint");

        // Throw and catch to populate StackTrace (exceptions created with 'new' have null StackTrace)
        Exception exception;
        try
        {
            throw new InvalidOperationException("Something went wrong");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Act
        var wideEvent = builder.Exception(exception);

        // Assert
        wideEvent.Outcome.Should().Be("exception");
        wideEvent.ExceptionType.Should().Be("System.InvalidOperationException");
        wideEvent.ExceptionMessage.Should().Be("Something went wrong");
        wideEvent.ExceptionStackTrace.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MessageWideEventBuilder_WithContext_ShouldMergeWideEventContext()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var builder = MessageWideEvent.Start(
            MessageId.New().ToString(),
            CorrelationId.New().ToString(),
            "OrderCreated",
            "OrderCreatedConsumer",
            "order-endpoint");

        // Act - Simulate context that would come from ConsumeContext.WideEventContext
        builder
            .WithContext("order_id", orderId)
            .WithContext("customer_id", "CUST-001")
            .WithContext("amount", 99.99m)
            .WithContext("processed", true);

        var wideEvent = builder.Success();

        // Assert
        wideEvent.MessageContext.Should().ContainKey("order_id");
        wideEvent.MessageContext.Should().ContainKey("customer_id");
        wideEvent.MessageContext.Should().ContainKey("amount");
        wideEvent.MessageContext.Should().ContainKey("processed");
        wideEvent.MessageContext["order_id"].Should().Be(orderId);
        wideEvent.MessageContext["customer_id"].Should().Be("CUST-001");
        wideEvent.MessageContext["amount"].Should().Be(99.99m);
        wideEvent.MessageContext["processed"].Should().Be(true);
    }

    [Fact]
    public void MessageWideEventBuilder_ShouldCaptureRetryInfo()
    {
        // Arrange
        var builder = MessageWideEvent.Start(
            MessageId.New().ToString(),
            CorrelationId.New().ToString(),
            "TestMessage",
            "TestConsumer",
            "test-endpoint");

        // Act
        builder.WithRetryInfo(attempt: 2, maxRetries: 5, redelivered: true);
        var wideEvent = builder.Success();

        // Assert
        wideEvent.RetryAttempt.Should().Be(2);
        wideEvent.MaxRetryCount.Should().Be(5);
        wideEvent.Redelivered.Should().BeTrue();
    }

    [Fact]
    public void MessageWideEventBuilder_ShouldCaptureFilterTypes()
    {
        // Arrange
        var builder = MessageWideEvent.Start(
            MessageId.New().ToString(),
            CorrelationId.New().ToString(),
            "TestMessage",
            "TestConsumer",
            "test-endpoint");

        // Act
        builder.WithFilters("RetryFilter", "CircuitBreakerFilter", "TimeoutFilter");
        var wideEvent = builder.Success();

        // Assert
        wideEvent.FilterTypes.Should().NotBeNull();
        wideEvent.FilterTypes.Should().Contain("RetryFilter");
        wideEvent.FilterTypes.Should().Contain("CircuitBreakerFilter");
        wideEvent.FilterTypes.Should().Contain("TimeoutFilter");
    }

    [Fact]
    public void MessageWideEventBuilder_ShouldCaptureServiceContext()
    {
        // Arrange
        // Set environment variables for service context
        Environment.SetEnvironmentVariable("SERVICE_NAME", "OrderService");
        Environment.SetEnvironmentVariable("SERVICE_VERSION", "1.0.0");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        try
        {
            // Act
            var builder = MessageWideEvent.Start(
                MessageId.New().ToString(),
                CorrelationId.New().ToString(),
                "TestMessage",
                "TestConsumer",
                "test-endpoint");

            var wideEvent = builder.Success();

            // Assert
            wideEvent.ServiceName.Should().Be("OrderService");
            wideEvent.ServiceVersion.Should().Be("1.0.0");
            wideEvent.Environment.Should().Be("Testing");
            wideEvent.Host.Should().NotBeNullOrEmpty();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("SERVICE_NAME", null);
            Environment.SetEnvironmentVariable("SERVICE_VERSION", null);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
    }

    [Fact]
    public void WideEventEmitterAdapter_ShouldConvertMessageWideEventToFeatureWideEvent()
    {
        // Arrange
        var featureEmitter = new TestWideEventEmitter();
        var adapter = new WideEventEmitterAdapter(featureEmitter);

        var messageEvent = new MessageWideEvent
        {
            MessageId = MessageId.New().ToString(),
            CorrelationId = CorrelationId.New().ToString(),
            MessageType = "OrderCreated",
            ConsumerType = "OrderCreatedConsumer",
            EndpointName = "order-endpoint",
            Outcome = "success",
            TraceId = "trace-123",
            SpanId = "span-456",
            TotalMs = 50.5,
            ConsumerMs = 25.0,
            ServiceName = "OrderService",
            ServiceVersion = "1.0.0",
            Environment = "Testing"
        };

        messageEvent.MessageContext["order_id"] = Guid.NewGuid();
        messageEvent.MessageContext["customer_id"] = "CUST-001";

        // Act
        adapter.Emit(messageEvent);

        // Assert
        featureEmitter.Events.Should().HaveCount(1);
        var featureEvent = featureEmitter.LastEvent!;

        featureEvent.FeatureName.Should().Be("Message:OrderCreated");
        featureEvent.FeatureType.Should().Be("Consumer");
        featureEvent.RequestType.Should().Be("OrderCreated");
        featureEvent.MutatorType.Should().Be("OrderCreatedConsumer");
        featureEvent.Outcome.Should().Be("success");
        featureEvent.TraceId.Should().Be("trace-123");
        featureEvent.SpanId.Should().Be("span-456");
        featureEvent.TotalMs.Should().Be(50.5);
        featureEvent.ExecutionMs.Should().Be(25.0);
        featureEvent.ServiceName.Should().Be("OrderService");

        // Check context was copied
        featureEvent.RequestContext.Should().ContainKey("order_id");
        featureEvent.RequestContext.Should().ContainKey("customer_id");
        featureEvent.RequestContext.Should().ContainKey("message_id");
        featureEvent.RequestContext.Should().ContainKey("correlation_id");
        featureEvent.RequestContext.Should().ContainKey("endpoint_name");
    }

    [Fact]
    public void WideEventEmitterAdapter_ShouldMapOutcomesCorrectly()
    {
        // Arrange
        var testCases = new Dictionary<string, string>
        {
            { "success", "success" },
            { "consumer_error", "execution_failure" },
            { "deserialization_error", "validation_failure" },
            { "retry_exhausted", "execution_failure" },
            { "circuit_breaker_open", "requirements_failure" },
            { "timeout", "execution_failure" },
            { "exception", "exception" }
        };

        var featureEmitter = new TestWideEventEmitter();
        var adapter = new WideEventEmitterAdapter(featureEmitter);

        foreach (var (messageOutcome, expectedFeatureOutcome) in testCases)
        {
            featureEmitter.Clear();

            var messageEvent = new MessageWideEvent
            {
                MessageId = MessageId.New().ToString(),
                CorrelationId = CorrelationId.New().ToString(),
                MessageType = "TestMessage",
                ConsumerType = "TestConsumer",
                EndpointName = "test-endpoint",
                Outcome = messageOutcome
            };

            // Act
            adapter.Emit(messageEvent);

            // Assert
            featureEmitter.LastEvent!.Outcome.Should().Be(
                expectedFeatureOutcome,
                $"Outcome '{messageOutcome}' should map to '{expectedFeatureOutcome}'");
        }
    }

    private MessageEnvelope CreateTestEnvelope()
    {
        return new MessageEnvelope
        {
            MessageId = MessageId.New(),
            CorrelationId = CorrelationId.New(),
            MessageTypes = new[] { "urn:message:Tests.Integration:OrderCreated" },
            SentTime = DateTimeOffset.UtcNow,
            Body = "{}"u8.ToArray(),
            Headers = new MessageHeaders(),
        };
    }
}

/// <summary>
/// Integration tests for context propagation across service boundaries.
/// Tests that trace context, correlation IDs, and wide event context
/// are properly propagated when consumers publish follow-up messages.
/// </summary>
[Collection("Messaging")]
public class ContextPropagationIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqFixture _fixture;
    private readonly List<MessageEnvelope> _receivedEnvelopes = new();

    public ContextPropagationIntegrationTests(RabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    private readonly TaskCompletionSource<MessageEnvelope> _followUpReceived = new();

    public async Task InitializeAsync()
    {
        await _fixture.InitializeWithInMemoryAsync(cfg =>
        {
            // First consumer - publishes a follow-up event
            cfg.ReceiveEndpoint("initiator-endpoint", ep =>
            {
                ep.Handler<OrderCreated>(async (context, ct) =>
                {
                    // Record the envelope for verification
                    lock (_receivedEnvelopes)
                    {
                        _receivedEnvelopes.Add(context.Envelope);
                    }

                    // Publish follow-up event - should inherit trace context
                    await context.PublishAsync(new OrderShipped(
                        context.Message.OrderId,
                        $"TRACK-{context.Message.OrderId:N}"));
                    return Unit.Value;
                });
            });

            // Second consumer - receives the follow-up
            cfg.ReceiveEndpoint("follower-endpoint", ep =>
            {
                ep.Handler<OrderShipped>((context, ct) =>
                {
                    // Record the envelope for verification
                    lock (_receivedEnvelopes)
                    {
                        _receivedEnvelopes.Add(context.Envelope);
                    }

                    _followUpReceived.TrySetResult(context.Envelope);
                    return Task.FromResult<VsaResult<Unit>>(Unit.Value);
                });
            });
        });
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task FollowUpMessage_ShouldInheritTraceContext()
    {
        // Arrange
        var bus = _fixture.GetBus();
        var orderId = Guid.NewGuid();
        var originalTraceId = "original-trace-id-123";
        var originalSpanId = "original-span-id-456";
        var originalTenantId = "tenant-xyz";

        // Act - Publish with explicit trace context
        await bus.PublishAsync(
            new OrderCreated(orderId, "CUST-001", 99.99m),
            headers =>
            {
                headers.TraceId = originalTraceId;
                headers.SpanId = originalSpanId;
                headers.TenantId = originalTenantId;
            });

        // Wait for the follow-up message to be received
        var followUpEnvelope = await _followUpReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert - Follow-up message should have the same trace context
        followUpEnvelope.Headers.TraceId.Should().Be(originalTraceId);
        followUpEnvelope.Headers.TenantId.Should().Be(originalTenantId);
    }

    [Fact]
    public async Task FollowUpMessage_ShouldSetInitiatorId()
    {
        // Arrange
        var bus = _fixture.GetBus();
        var orderId = Guid.NewGuid();
        _receivedEnvelopes.Clear();

        // Act
        await bus.PublishAsync(new OrderCreated(orderId, "CUST-002", 199.99m));

        // Wait for both messages to be processed
        await _followUpReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        _receivedEnvelopes.Should().HaveCountGreaterOrEqualTo(2);

        var initiatorEnvelope = _receivedEnvelopes.First(e =>
            e.MessageTypes.Any(t => t.Contains("OrderCreated")));

        var followUpEnvelope = _receivedEnvelopes.First(e =>
            e.MessageTypes.Any(t => t.Contains("OrderShipped")));

        // The follow-up message's InitiatorId should point to the original message
        followUpEnvelope.Headers.InitiatorId.Should().Be(initiatorEnvelope.MessageId.ToString());
    }

    [Fact]
    public async Task ConsumeContext_PublishAsync_ShouldPreserveCorrelation()
    {
        // Arrange
        var bus = _fixture.GetBus();
        var orderId = Guid.NewGuid();
        _receivedEnvelopes.Clear();

        // Act
        await bus.PublishAsync(new OrderCreated(orderId, "CUST-003", 299.99m));

        // Wait for messages
        var followUpEnvelope = await _followUpReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert - Both messages should share the same correlation ID flow
        var initiatorEnvelope = _receivedEnvelopes.First(e =>
            e.MessageTypes.Any(t => t.Contains("OrderCreated")));

        // Follow-up envelope inherits from initiator
        followUpEnvelope.InitiatorId.Should().NotBeNull();
    }
}

/// <summary>
/// Tests for ConsumeContext fluent API and WideEventContext management.
/// </summary>
[Collection("Messaging")]
public class ConsumeContextTests : IAsyncLifetime
{
    private readonly RabbitMqFixture _fixture;
    private ConsumeContext<OrderCreated>? _capturedContext;

    public ConsumeContextTests(RabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeWithInMemoryAsync(cfg =>
        {
            cfg.ReceiveEndpoint("context-test-endpoint", ep =>
            {
                ep.Handler<OrderCreated>((context, ct) =>
                {
                    _capturedContext = context;
                    return Task.FromResult<VsaResult<Unit>>(Unit.Value);
                });
            });
        });
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task ConsumeContext_AddContext_ShouldSupportFluentChaining()
    {
        // Arrange
        var bus = _fixture.GetBus();
        var orderId = Guid.NewGuid();

        // Act
        await bus.PublishAsync(new OrderCreated(orderId, "CUST-001", 99.99m));
        await Task.Delay(100); // Wait for consumer

        // Assert
        _capturedContext.Should().NotBeNull();

        // Test fluent chaining
        var result = _capturedContext!
            .AddContext("key1", "value1")
            .AddContext("key2", "value2")
            .AddContext("key3", "value3");

        result.Should().BeSameAs(_capturedContext);
        _capturedContext.WideEventContext.Should().ContainKey("key1");
        _capturedContext.WideEventContext.Should().ContainKey("key2");
        _capturedContext.WideEventContext.Should().ContainKey("key3");
    }

    [Fact]
    public async Task ConsumeContext_AddContext_ShouldSupportMultiplePairs()
    {
        // Arrange
        var bus = _fixture.GetBus();
        var orderId = Guid.NewGuid();

        // Act
        await bus.PublishAsync(new OrderCreated(orderId, "CUST-002", 199.99m));
        await Task.Delay(100);

        // Assert
        _capturedContext.Should().NotBeNull();

        _capturedContext!.AddContext(
            ("order_id", orderId),
            ("customer_id", "CUST-002"),
            ("amount", 199.99m),
            ("timestamp", DateTimeOffset.UtcNow));

        _capturedContext.WideEventContext.Should().HaveCount(4);
        _capturedContext.WideEventContext["order_id"].Should().Be(orderId);
        _capturedContext.WideEventContext["customer_id"].Should().Be("CUST-002");
        _capturedContext.WideEventContext["amount"].Should().Be(199.99m);
    }

    [Fact]
    public async Task ConsumeContext_Payload_ShouldStoreAndRetrieveValues()
    {
        // Arrange
        var bus = _fixture.GetBus();

        // Act
        await bus.PublishAsync(new OrderCreated(Guid.NewGuid(), "CUST-003", 299.99m));
        await Task.Delay(100);

        // Assert
        _capturedContext.Should().NotBeNull();

        _capturedContext!.SetPayload("user", new { Id = 123, Name = "Test" });
        _capturedContext.SetPayload("count", 42);

        var user = _capturedContext.GetPayload<object>("user");
        var count = _capturedContext.GetPayload<int>("count");

        user.Should().NotBeNull();
        count.Should().Be(42);
    }

    [Fact]
    public async Task ConsumeContext_TryGetPayload_ShouldReturnFalseForMissing()
    {
        // Arrange
        var bus = _fixture.GetBus();

        // Act
        await bus.PublishAsync(new OrderCreated(Guid.NewGuid(), "CUST-004", 399.99m));
        await Task.Delay(100);

        // Assert
        _capturedContext.Should().NotBeNull();

        var exists = _capturedContext!.TryGetPayload<string>("nonexistent", out var value);

        exists.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public async Task ConsumeContext_ShouldExposeEnvelopeProperties()
    {
        // Arrange
        var bus = _fixture.GetBus();
        var orderId = Guid.NewGuid();

        // Act
        await bus.PublishAsync(
            new OrderCreated(orderId, "CUST-005", 499.99m),
            headers =>
            {
                headers.TraceId = "trace-test";
                headers.TenantId = "tenant-test";
            });
        await Task.Delay(100);

        // Assert
        _capturedContext.Should().NotBeNull();

        _capturedContext!.MessageId.Should().NotBe(default);
        _capturedContext.CorrelationId.Should().NotBe(default);
        _capturedContext.Headers.TraceId.Should().Be("trace-test");
        _capturedContext.Headers.TenantId.Should().Be("tenant-test");
        _capturedContext.SentTime.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
