using FluentAssertions;
using VsaResults;
using VsaResults.WideEvents;
using Xunit;

namespace Tests.WideEvents;

/// <summary>
/// Unit tests for the unified wide events system including WideEventScope,
/// UnifiedWideEventEmitter, and the interceptor pipeline.
/// </summary>
public class WideEventScopeTests
{
    [Fact]
    public void Current_ShouldBeNullWhenNoScopeActive()
    {
        // Arrange & Act
        var current = WideEventScope.Current;

        // Assert
        current.Should().BeNull();
        WideEventScope.IsActive.Should().BeFalse();
    }

    [Fact]
    public void WhenCreated_ShouldSetCurrent()
    {
        // Arrange & Act
        using var scope = new WideEventScope("message");

        // Assert
        WideEventScope.Current.Should().BeSameAs(scope);
        WideEventScope.IsActive.Should().BeTrue();
    }

    [Fact]
    public void WhenDisposed_ShouldClearCurrent()
    {
        // Arrange
        var scope = new WideEventScope("message");

        // Act
        scope.Dispose();

        // Assert
        WideEventScope.Current.Should().BeNull();
        WideEventScope.IsActive.Should().BeFalse();
    }

    [Fact]
    public void WhenNested_ShouldTrackParent()
    {
        // Arrange & Act
        using var outer = new WideEventScope("message");
        using var inner = new WideEventScope("feature");

        // Assert
        inner.Parent.Should().BeSameAs(outer);
        WideEventScope.Current.Should().BeSameAs(inner);
    }

    [Fact]
    public void WhenNestedDisposed_ShouldRestoreParent()
    {
        // Arrange
        using var outer = new WideEventScope("message");

        // Act
        using (var inner = new WideEventScope("feature"))
        {
            WideEventScope.Current.Should().BeSameAs(inner);
        }

        // Assert
        WideEventScope.Current.Should().BeSameAs(outer);
    }

    [Fact]
    public void AggregateToParent_ShouldCaptureChildEvents()
    {
        // Arrange
        using var scope = new WideEventScope("message", WideEventAggregationMode.AggregateToParent);
        var childEvent = new WideEvent
        {
            EventType = "feature",
            Outcome = "success",
        };

        // Act
        var captured = scope.TryCaptureChildEvent(childEvent);

        // Assert
        captured.Should().BeTrue();
    }

    [Fact]
    public void LinkOnly_ShouldNotCaptureButSetCausationId()
    {
        // Arrange
        using var scope = new WideEventScope("message", WideEventAggregationMode.LinkOnly);
        var childEvent = new WideEvent
        {
            EventType = "feature",
            Outcome = "success",
        };

        // Act
        var captured = scope.TryCaptureChildEvent(childEvent);

        // Assert
        captured.Should().BeFalse();
        childEvent.CausationId.Should().Be(scope.EventId);
    }

    [Fact]
    public void Independent_ShouldNotCaptureOrModify()
    {
        // Arrange
        using var scope = new WideEventScope("message", WideEventAggregationMode.Independent);
        var childEvent = new WideEvent
        {
            EventType = "feature",
            Outcome = "success",
        };

        // Act
        var captured = scope.TryCaptureChildEvent(childEvent);

        // Assert
        captured.Should().BeFalse();
        childEvent.CausationId.Should().BeNull();
    }

    [Fact]
    public void TryReportToCurrentScope_ShouldWorkWithActiveScope()
    {
        // Arrange
        var childEvent = new WideEvent
        {
            EventType = "feature",
            Outcome = "success",
        };

        // Act & Assert - no scope
        WideEventScope.TryReportToCurrentScope(childEvent).Should().BeFalse();

        // Act & Assert - with scope
        using var scope = new WideEventScope("message");
        WideEventScope.TryReportToCurrentScope(childEvent).Should().BeTrue();
    }

    [Fact]
    public void CompleteSuccess_ShouldBuildEventWithChildSpans()
    {
        // Arrange
        using var scope = new WideEventScope("message");

        // Add a child event
        var childEvent = new WideEvent
        {
            EventType = "feature",
            Outcome = "success",
            TotalMs = 50,
        };
        childEvent.Feature = new WideEventFeatureSegment
        {
            FeatureName = "TestFeature",
            FeatureType = "Mutation",
        };
        scope.TryCaptureChildEvent(childEvent);

        // Act
        var result = scope.CompleteSuccess();

        // Assert
        result.Outcome.Should().Be("success");
        result.ChildSpans.Should().ContainSingle();
        result.ChildSpans[0].Name.Should().Be("TestFeature");
        result.ChildSpans[0].Outcome.Should().Be("success");
    }

    [Fact]
    public void CompleteValidationFailure_ShouldSetErrorInfo()
    {
        // Arrange
        using var scope = new WideEventScope("message");
        var errors = new List<Error>
        {
            Error.Validation("Name.Required", "Name is required"),
            Error.Validation("Email.Invalid", "Email format is invalid"),
        };

        // Act
        var result = scope.CompleteValidationFailure(errors);

        // Assert
        result.Outcome.Should().Be("validation_failure");
        result.Error.Should().NotBeNull();
        result.Error!.Count.Should().Be(2);
        result.Error.Code.Should().Be("Name.Required");
        result.Error.Message.Should().Be("Name is required");
    }

    [Fact]
    public void CompleteException_ShouldSetExceptionInfo()
    {
        // Arrange
        using var scope = new WideEventScope("message");
        Exception exception;
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Act
        var result = scope.CompleteException(exception, includeStackTrace: true);

        // Assert
        result.Outcome.Should().Be("exception");
        result.Error.Should().NotBeNull();
        result.Error!.ExceptionType.Should().Contain("InvalidOperationException");
        result.Error.ExceptionMessage.Should().Be("Test exception");
        result.Error.ExceptionStackTrace.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ShouldFlowAcrossAsyncBoundaries()
    {
        // Arrange
        using var scope = new WideEventScope("message");
        var capturedScope = WideEventScope.Current;

        // Act - simulate async operation
        await Task.Delay(1);
        var afterAsyncScope = WideEventScope.Current;

        // Assert
        afterAsyncScope.Should().BeSameAs(capturedScope);
    }
}

/// <summary>
/// Unit tests for WideEvent and WideEventBuilder.
/// </summary>
public class WideEventTests
{
    [Fact]
    public void StartFeature_ShouldCreateBuilderWithFeatureSegment()
    {
        // Arrange & Act
        var builder = WideEvent.StartFeature("CreateOrder", "Mutation");
        var wideEvent = builder.Success();

        // Assert
        wideEvent.EventType.Should().Be("feature");
        wideEvent.Feature.Should().NotBeNull();
        wideEvent.Feature!.FeatureName.Should().Be("CreateOrder");
        wideEvent.Feature.FeatureType.Should().Be("Mutation");
    }

    [Fact]
    public void StartMessage_ShouldCreateBuilderWithMessageSegment()
    {
        // Arrange & Act
        var builder = WideEvent.StartMessage("msg-123", "OrderCreated", "OrderConsumer", "orders-queue");
        var wideEvent = builder.Success();

        // Assert
        wideEvent.EventType.Should().Be("message");
        wideEvent.Message.Should().NotBeNull();
        wideEvent.Message!.MessageId.Should().Be("msg-123");
        wideEvent.Message.MessageType.Should().Be("OrderCreated");
        wideEvent.Message.ConsumerType.Should().Be("OrderConsumer");
        wideEvent.Message.EndpointName.Should().Be("orders-queue");
    }

    [Fact]
    public void IsSuccess_ShouldReflectOutcome()
    {
        // Arrange & Act
        var successEvent = new WideEvent { EventType = "test", Outcome = "success" };
        var failureEvent = new WideEvent { EventType = "test", Outcome = "validation_failure" };

        // Assert
        successEvent.IsSuccess.Should().BeTrue();
        failureEvent.IsSuccess.Should().BeFalse();
    }
}

/// <summary>
/// Unit tests for WideEventBuilder.
/// </summary>
public class WideEventBuilderTests
{
    [Fact]
    public void WithContext_ShouldAccumulateValues()
    {
        // Arrange
        var builder = new WideEventBuilder("feature");

        // Act
        builder
            .WithContext("order_id", "ORD-123")
            .WithContext("customer_id", "CUST-456")
            .WithContext("amount", 99.99);

        var wideEvent = builder.Success();

        // Assert
        wideEvent.Context.Should().HaveCount(3);
        wideEvent.Context["order_id"].Should().Be("ORD-123");
        wideEvent.Context["customer_id"].Should().Be("CUST-456");
        wideEvent.Context["amount"].Should().Be(99.99);
    }

    [Fact]
    public void WithCorrelationId_ShouldSetValue()
    {
        // Arrange
        var builder = new WideEventBuilder("message");

        // Act
        builder.WithCorrelationId("corr-123");
        var wideEvent = builder.Success();

        // Assert
        wideEvent.CorrelationId.Should().Be("corr-123");
    }

    [Fact]
    public void WithCausationId_ShouldSetValue()
    {
        // Arrange
        var builder = new WideEventBuilder("feature");

        // Act
        builder.WithCausationId("cause-123");
        var wideEvent = builder.Success();

        // Assert
        wideEvent.CausationId.Should().Be("cause-123");
    }

    [Fact]
    public void RecordTimings_ShouldCaptureStageMs()
    {
        // Arrange
        var builder = WideEvent.StartFeature("Test", "Mutation");

        // Act - simulate stage timings
        builder.StartStage("validation");
        Thread.Sleep(5);
        builder.RecordValidation();

        builder.StartStage("execution");
        Thread.Sleep(5);
        builder.RecordExecution();

        var wideEvent = builder.Success();

        // Assert
        wideEvent.Feature!.ValidationMs.Should().BeGreaterThan(0);
        wideEvent.Feature.ExecutionMs.Should().BeGreaterThan(0);
        wideEvent.TotalMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ValidationFailure_ShouldSetOutcomeAndErrors()
    {
        // Arrange
        var builder = WideEvent.StartFeature("Test", "Mutation");
        var errors = new List<Error>
        {
            Error.Validation("Field.Required", "Field is required"),
        };

        // Act
        var wideEvent = builder.ValidationFailure(errors);

        // Assert
        wideEvent.Outcome.Should().Be("validation_failure");
        wideEvent.IsSuccess.Should().BeFalse();
        wideEvent.Error.Should().NotBeNull();
        wideEvent.Error!.Code.Should().Be("Field.Required");
    }

    [Fact]
    public void Exception_ShouldCaptureExceptionInfo()
    {
        // Arrange
        var builder = WideEvent.StartFeature("Test", "Mutation");
        builder.StartStage("execution");
        Exception ex;
        try
        {
            throw new InvalidOperationException("Something went wrong");
        }
        catch (Exception e)
        {
            ex = e;
        }

        // Act
        var wideEvent = builder.Exception(ex, includeStackTrace: true);

        // Assert
        wideEvent.Outcome.Should().Be("exception");
        wideEvent.Error.Should().NotBeNull();
        wideEvent.Error!.ExceptionType.Should().Contain("InvalidOperationException");
        wideEvent.Error.ExceptionMessage.Should().Be("Something went wrong");
        wideEvent.Error.ExceptionStackTrace.Should().NotBeNull();
    }
}

/// <summary>
/// Unit tests for UnifiedWideEventEmitter.
/// </summary>
public class UnifiedWideEventEmitterTests
{
    [Fact]
    public async Task EmitAsync_ShouldWriteToSink()
    {
        // Arrange
        var sink = new InMemoryWideEventSink();
        var emitter = new UnifiedWideEventEmitter(sink);
        var wideEvent = new WideEvent { EventType = "test", Outcome = "success" };

        // Act
        await emitter.EmitAsync(wideEvent);

        // Assert
        sink.Events.Should().ContainSingle();
        sink.Events[0].Should().BeSameAs(wideEvent);
    }

    [Fact]
    public async Task EmitAsync_ShouldCaptureInActiveScope()
    {
        // Arrange
        var sink = new InMemoryWideEventSink();
        var emitter = new UnifiedWideEventEmitter(sink);
        var childEvent = new WideEvent { EventType = "feature", Outcome = "success" };

        // Act - emit within scope
        using var scope = new WideEventScope("message");
        await emitter.EmitAsync(childEvent);

        // Assert - event should be captured, not emitted
        sink.Events.Should().BeEmpty();
    }

    [Fact]
    public void Emit_ShouldWorkSynchronously()
    {
        // Arrange
        var sink = new InMemoryWideEventSink();
        var emitter = new UnifiedWideEventEmitter(sink);
        var wideEvent = new WideEvent { EventType = "test", Outcome = "success" };

        // Act
        emitter.Emit(wideEvent);

        // Assert
        sink.Events.Should().ContainSingle();
    }

    [Fact]
    public async Task Create_ShouldConfigureInterceptors()
    {
        // Arrange
        var sink = new InMemoryWideEventSink();
        var options = WideEventOptions.Development();
        var emitter = UnifiedWideEventEmitter.Create(sink, options);
        var wideEvent = new WideEvent { EventType = "test", Outcome = "success" };

        // Act
        await emitter.EmitAsync(wideEvent);

        // Assert - event should be emitted (dev options don't filter success events)
        sink.Events.Should().ContainSingle();
    }
}

/// <summary>
/// Unit tests for interceptors.
/// </summary>
public class InterceptorTests
{
    [Fact]
    public async Task SamplingInterceptor_ShouldAlwaysCaptureErrors()
    {
        // Arrange
        var options = new WideEventOptions
        {
            SamplingRate = 0.0,
            EnableSampling = true,
        };
        var interceptor = new SamplingInterceptor(options);
        var errorEvent = new WideEvent { EventType = "test", Outcome = "exception" };
        var successEvent = new WideEvent { EventType = "test", Outcome = "success" };

        // Act
        var errorResult = await interceptor.OnBeforeEmitAsync(errorEvent, CancellationToken.None);
        var successResult = await interceptor.OnBeforeEmitAsync(successEvent, CancellationToken.None);

        // Assert
        errorResult.Should().NotBeNull("error events are always captured");
        successResult.Should().BeNull("success events are sampled at 0%");
    }

    [Fact]
    public async Task RedactionInterceptor_ShouldRedactSensitiveKeys()
    {
        // Arrange
        var options = new WideEventOptions();
        options.ExcludedContextKeys.Add("secret_key");
        var interceptor = new RedactionInterceptor(options);
        var wideEvent = new WideEvent
        {
            EventType = "test",
            Outcome = "success",
            Context = new Dictionary<string, object?>
            {
                ["user_id"] = "user-123",
                ["secret_key"] = "super-secret",
                ["password"] = "should-be-redacted",
            },
        };

        // Act
        var result = await interceptor.OnBeforeEmitAsync(wideEvent, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Context["user_id"].Should().Be("user-123");
        result.Context["secret_key"].Should().Be("[REDACTED]");
        result.Context["password"].Should().Be("[REDACTED]");
    }

    [Fact]
    public async Task ContextLimitInterceptor_ShouldTruncateOversizedContext()
    {
        // Arrange
        var options = new WideEventOptions
        {
            MaxContextEntries = 3,
            MaxStringValueLength = 10,
        };
        var interceptor = new ContextLimitInterceptor(options);
        var wideEvent = new WideEvent
        {
            EventType = "test",
            Outcome = "success",
            Context = new Dictionary<string, object?>
            {
                ["key1"] = "value1",
                ["key2"] = "value2",
                ["key3"] = "value3",
                ["key4"] = "value4",
                ["key5"] = "this is a very long string that should be truncated",
            },
        };

        // Act
        var result = await interceptor.OnBeforeEmitAsync(wideEvent, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Context.Should().HaveCount(3);
    }

    [Fact]
    public async Task VerbosityInterceptor_Minimal_ShouldStripNonEssentialFields()
    {
        // Arrange
        var options = new WideEventOptions { Verbosity = WideEventVerbosity.Minimal };
        var interceptor = new VerbosityInterceptor(options);
        var wideEvent = new WideEvent
        {
            EventType = "feature",
            Outcome = "success",
            Feature = new WideEventFeatureSegment
            {
                FeatureName = "Test",
                FeatureType = "Mutation",
                ValidatorType = "TestValidator",
            },
            Context = new Dictionary<string, object?> { ["key"] = "value" },
        };

        // Act
        var result = await interceptor.OnBeforeEmitAsync(wideEvent, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Minimal verbosity strips context and detailed feature info
        result!.Context.Should().BeEmpty();
    }
}

/// <summary>
/// Unit tests for WideEventOptions.
/// </summary>
public class WideEventOptionsTests
{
    [Fact]
    public void Development_ShouldConfigureForDebugging()
    {
        // Arrange & Act
        var options = WideEventOptions.Development();

        // Assert
        options.Verbosity.Should().Be(WideEventVerbosity.Verbose);
        options.EnableSampling.Should().BeFalse();
        options.IncludeStackTraces.Should().BeTrue();
        options.MaxContextEntries.Should().Be(100);
    }

    [Fact]
    public void Production_ShouldConfigureForCostEfficiency()
    {
        // Arrange & Act
        var options = WideEventOptions.Production();

        // Assert
        options.Verbosity.Should().Be(WideEventVerbosity.Standard);
        options.SamplingRate.Should().Be(0.1);
        options.EnableSampling.Should().BeTrue();
        options.IncludeStackTraces.Should().BeFalse();
    }

    [Fact]
    public void HighThroughput_ShouldConfigureForMinimalOverhead()
    {
        // Arrange & Act
        var options = WideEventOptions.HighThroughput();

        // Assert
        options.Verbosity.Should().Be(WideEventVerbosity.Minimal);
        options.SamplingRate.Should().Be(0.01);
        options.MaxContextEntries.Should().Be(20);
        options.MaxChildSpans.Should().Be(5);
        options.IncludeChildSpanContext.Should().BeFalse();
    }

    [Fact]
    public void AlwaysCaptureOutcomes_ShouldIncludeErrorTypes()
    {
        // Arrange & Act
        var options = new WideEventOptions();

        // Assert
        options.AlwaysCaptureOutcomes.Should().Contain("exception");
        options.AlwaysCaptureOutcomes.Should().Contain("validation_failure");
        options.AlwaysCaptureOutcomes.Should().Contain("requirements_failure");
        options.AlwaysCaptureOutcomes.Should().Contain("consumer_error");
    }

    [Fact]
    public void ExcludedContextKeys_ShouldIncludeSensitiveDefaults()
    {
        // Arrange & Act
        var options = new WideEventOptions();

        // Assert
        options.ExcludedContextKeys.Should().Contain("password");
        options.ExcludedContextKeys.Should().Contain("token");
        options.ExcludedContextKeys.Should().Contain("secret");
        options.ExcludedContextKeys.Should().Contain("api_key");
    }
}

/// <summary>
/// Unit tests for WideEventChildSpan.
/// </summary>
public class WideEventChildSpanTests
{
    [Fact]
    public void FromWideEvent_ShouldConvertCorrectly()
    {
        // Arrange
        var wideEvent = new WideEvent
        {
            EventType = "feature",
            Outcome = "success",
            TotalMs = 123.45,
            Feature = new WideEventFeatureSegment
            {
                FeatureName = "CreateOrder",
                FeatureType = "Mutation",
            },
        };

        // Act
        var childSpan = WideEventChildSpan.FromWideEvent(wideEvent);

        // Assert
        childSpan.EventType.Should().Be("feature");
        childSpan.Name.Should().Be("CreateOrder");
        childSpan.Outcome.Should().Be("success");
        childSpan.DurationMs.Should().Be(123.45);
        childSpan.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void FromWideEvent_WithMessage_ShouldUseMessageType()
    {
        // Arrange
        var wideEvent = new WideEvent
        {
            EventType = "message",
            Outcome = "consumer_error",
            Message = new WideEventMessageSegment
            {
                MessageType = "OrderCreated",
                ConsumerType = "OrderConsumer",
                EndpointName = "orders",
                MessageId = "msg-123",
            },
        };

        // Act
        var childSpan = WideEventChildSpan.FromWideEvent(wideEvent);

        // Assert
        childSpan.Name.Should().Be("OrderCreated");
        childSpan.IsSuccess.Should().BeFalse();
    }
}

/// <summary>
/// Unit tests for WideEventErrorSegment.
/// </summary>
public class WideEventErrorSegmentTests
{
    [Fact]
    public void FromErrors_ShouldCaptureFirstError()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("First.Error", "First error message"),
            Error.Validation("Second.Error", "Second error message"),
        };

        // Act
        var segment = WideEventErrorSegment.FromErrors(errors, "validation");

        // Assert
        segment.Code.Should().Be("First.Error");
        segment.Message.Should().Be("First error message");
        segment.Count.Should().Be(2);
        segment.FailedAtStage.Should().Be("validation");
        segment.AllDescriptions.Should().Contain("First.Error");
        segment.AllDescriptions.Should().Contain("Second.Error");
    }

    [Fact]
    public void FromException_ShouldCaptureExceptionDetails()
    {
        // Arrange - throw exception so it has a stack trace
        Exception exception;
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Act
        var segment = WideEventErrorSegment.FromException(exception, "execution", includeStackTrace: true);

        // Assert
        segment.ExceptionType.Should().Contain("InvalidOperationException");
        segment.ExceptionMessage.Should().Be("Test exception");
        segment.ExceptionStackTrace.Should().NotBeNullOrEmpty();
        segment.FailedAtStage.Should().Be("execution");
        segment.Count.Should().Be(1);
    }
}
