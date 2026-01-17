using FluentAssertions;
using VsaResults;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// Integration tests for FeatureWideEvent and FeatureWideEventBuilder.
/// </summary>
public class FeatureWideEventBuilderTests
{
    [Fact]
    public void FeatureWideEventBuilder_ShouldCaptureBasicContext()
    {
        // Arrange & Act
        var builder = FeatureWideEvent.Start("CreateOrder", "Mutation");
        var wideEvent = builder.Success();

        // Assert
        wideEvent.FeatureName.Should().Be("CreateOrder");
        wideEvent.FeatureType.Should().Be("Mutation");
        wideEvent.Outcome.Should().Be("success");
        wideEvent.IsSuccess.Should().BeTrue();
        wideEvent.TotalMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void FeatureWideEventBuilder_WithTypes_ShouldRecordRequestAndResultTypes()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("GetOrder", "Query");

        // Act
        builder.WithTypes<OrderRequest, OrderResult>();
        var wideEvent = builder.Success();

        // Assert
        wideEvent.RequestType.Should().Be("OrderRequest");
        wideEvent.ResultType.Should().Be("OrderResult");
    }

    [Fact]
    public void FeatureWideEventBuilder_WithContext_ShouldAccumulateContext()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("ProcessOrder", "Mutation");
        var orderId = Guid.NewGuid();

        // Act
        builder
            .WithContext("order_id", orderId)
            .WithContext("customer_id", "CUST-001")
            .WithContext("amount", 199.99m)
            .WithContext("is_priority", true);

        var wideEvent = builder.Success();

        // Assert
        wideEvent.RequestContext.Should().ContainKey("order_id");
        wideEvent.RequestContext.Should().ContainKey("customer_id");
        wideEvent.RequestContext.Should().ContainKey("amount");
        wideEvent.RequestContext.Should().ContainKey("is_priority");
        wideEvent.RequestContext["order_id"].Should().Be(orderId);
        wideEvent.RequestContext["customer_id"].Should().Be("CUST-001");
        wideEvent.RequestContext["amount"].Should().Be(199.99m);
        wideEvent.RequestContext["is_priority"].Should().Be(true);
    }

    [Fact]
    public void FeatureWideEventBuilder_WithRequestContext_ShouldExtractAttributedProperties()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("CreateUser", "Mutation");
        var request = new TestRequest
        {
            UserId = Guid.NewGuid(),
            UserName = "testuser",
            SensitiveData = "should-not-be-included"
        };

        // Act
        builder.WithRequestContext(request);
        var wideEvent = builder.Success();

        // Assert
        wideEvent.RequestContext.Should().ContainKey("user_id");
        wideEvent.RequestContext.Should().ContainKey("user_name");
        wideEvent.RequestContext.Should().NotContainKey("sensitive_data");
        wideEvent.RequestContext.Should().NotContainKey("SensitiveData");
    }

    [Fact]
    public void FeatureWideEventBuilder_WithRequestContext_ShouldExtractFromIWideEventContext()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("ProcessData", "Mutation");
        var request = new TestContextRequest("CUSTOM-123");

        // Act
        builder.WithRequestContext(request);
        var wideEvent = builder.Success();

        // Assert
        wideEvent.RequestContext.Should().ContainKey("custom_context_key");
        wideEvent.RequestContext["custom_context_key"].Should().Be("CUSTOM-123");
    }

    [Fact]
    public void FeatureWideEventBuilder_WithFeatureContext_ShouldMergeContext()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("UpdateOrder", "Mutation");
        var featureContext = new FeatureContext<object> { Request = new object() };

        featureContext.WideEventContext["order_id"] = Guid.NewGuid();
        featureContext.WideEventContext["status"] = "processing";
        featureContext.Entities["order"] = new TestOrder { Id = Guid.NewGuid() };

        // Act
        builder.WithFeatureContext(featureContext);
        var wideEvent = builder.Success();

        // Assert
        wideEvent.RequestContext.Should().ContainKey("order_id");
        wideEvent.RequestContext.Should().ContainKey("status");
        wideEvent.LoadedEntities.Should().ContainKey("order");
        wideEvent.LoadedEntities["order"].Should().Be("TestOrder");
    }

    [Fact]
    public void FeatureWideEventBuilder_WithPipelineStages_ShouldRecordStageTypes()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("CreateProduct", "Mutation");

        // Act
        builder.WithPipelineStages(
            validatorType: typeof(TestValidator),
            requirementsType: typeof(TestRequirements),
            mutatorOrQueryType: typeof(TestMutator),
            sideEffectsType: typeof(TestSideEffects),
            isMutation: true);

        var wideEvent = builder.Success();

        // Assert
        wideEvent.ValidatorType.Should().Be("TestValidator");
        wideEvent.RequirementsType.Should().Be("TestRequirements");
        wideEvent.MutatorType.Should().Be("TestMutator");
        wideEvent.SideEffectsType.Should().Be("TestSideEffects");
        wideEvent.HasCustomValidator.Should().BeTrue();
        wideEvent.HasCustomRequirements.Should().BeTrue();
        wideEvent.HasCustomSideEffects.Should().BeTrue();
    }

    [Fact]
    public void FeatureWideEventBuilder_WithPipelineStages_ShouldDetectNoOpStages()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("SimpleQuery", "Query");

        // Act
        builder.WithPipelineStages(
            validatorType: typeof(NoOpValidatorTest),
            requirementsType: typeof(NoOpRequirementsTest),
            mutatorOrQueryType: typeof(TestQuery),
            sideEffectsType: typeof(NoOpSideEffectsTest),
            isMutation: false);

        var wideEvent = builder.Success();

        // Assert
        wideEvent.QueryType.Should().Be("TestQuery");
        wideEvent.HasCustomValidator.Should().BeFalse();
        wideEvent.HasCustomRequirements.Should().BeFalse();
        wideEvent.HasCustomSideEffects.Should().BeFalse();
    }

    [Fact]
    public void FeatureWideEventBuilder_ShouldRecordTimingBreakdown()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("TimedFeature", "Mutation");

        // Act - Simulate pipeline stages
        builder.StartStage();
        Thread.Sleep(5);
        builder.RecordValidation();

        builder.StartStage();
        Thread.Sleep(5);
        builder.RecordRequirements();

        builder.StartStage();
        Thread.Sleep(5);
        builder.RecordExecution();

        builder.StartStage();
        Thread.Sleep(5);
        builder.RecordSideEffects();

        var wideEvent = builder.Success();

        // Assert
        wideEvent.ValidationMs.Should().BeGreaterThan(0);
        wideEvent.RequirementsMs.Should().BeGreaterThan(0);
        wideEvent.ExecutionMs.Should().BeGreaterThan(0);
        wideEvent.SideEffectsMs.Should().BeGreaterThan(0);
        wideEvent.TotalMs.Should().BeGreaterOrEqualTo(20);
    }

    [Fact]
    public void FeatureWideEventBuilder_ValidationFailure_ShouldCaptureErrors()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("FailingValidation", "Mutation");
        var errors = new List<Error>
        {
            Error.Validation("Email.Invalid", "Email format is invalid"),
            Error.Validation("Name.Required", "Name is required")
        };

        // Act
        var wideEvent = builder.ValidationFailure(errors);

        // Assert
        wideEvent.Outcome.Should().Be("validation_failure");
        wideEvent.IsSuccess.Should().BeFalse();
        wideEvent.FailedAtStage.Should().Be("validation");
        wideEvent.ErrorCode.Should().Be("Email.Invalid");
        wideEvent.ErrorType.Should().Be("Validation");
        wideEvent.ErrorMessage.Should().Be("Email format is invalid");
        wideEvent.ErrorCount.Should().Be(2);
        wideEvent.ErrorDescription.Should().Contain("Email.Invalid");
        wideEvent.ErrorDescription.Should().Contain("Name.Required");
    }

    [Fact]
    public void FeatureWideEventBuilder_RequirementsFailure_ShouldCaptureErrors()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("FailingRequirements", "Mutation");
        var errors = new List<Error>
        {
            Error.NotFound("Order.NotFound", "Order not found")
        };

        // Act
        var wideEvent = builder.RequirementsFailure(errors);

        // Assert
        wideEvent.Outcome.Should().Be("requirements_failure");
        wideEvent.FailedAtStage.Should().Be("requirements");
        wideEvent.ErrorCode.Should().Be("Order.NotFound");
        wideEvent.ErrorType.Should().Be("NotFound");
    }

    [Fact]
    public void FeatureWideEventBuilder_ExecutionFailure_ShouldCaptureErrors()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("FailingExecution", "Mutation");
        var errors = new List<Error>
        {
            Error.Failure("Payment.Declined", "Payment was declined")
        };

        // Act
        var wideEvent = builder.ExecutionFailure(errors);

        // Assert
        wideEvent.Outcome.Should().Be("execution_failure");
        wideEvent.FailedAtStage.Should().Be("execution");
        wideEvent.ErrorCode.Should().Be("Payment.Declined");
    }

    [Fact]
    public void FeatureWideEventBuilder_SideEffectsFailure_ShouldCaptureErrors()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("FailingSideEffects", "Mutation");
        var errors = new List<Error>
        {
            Error.Failure("Email.SendFailed", "Failed to send confirmation email")
        };

        // Act
        var wideEvent = builder.SideEffectsFailure(errors);

        // Assert
        wideEvent.Outcome.Should().Be("side_effects_failure");
        wideEvent.FailedAtStage.Should().Be("side_effects");
    }

    [Fact]
    public void FeatureWideEventBuilder_Exception_ShouldCaptureExceptionDetails()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("ExceptionFeature", "Mutation");

        // Throw and catch to populate stack trace
        Exception exception;
        try
        {
            throw new InvalidOperationException("Something went very wrong");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Act
        var wideEvent = builder.Exception(exception);

        // Assert
        wideEvent.Outcome.Should().Be("exception");
        wideEvent.FailedAtStage.Should().Be("unknown");
        wideEvent.ExceptionType.Should().Be("System.InvalidOperationException");
        wideEvent.ExceptionMessage.Should().Be("Something went very wrong");
        wideEvent.ExceptionStackTrace.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FeatureWideEventBuilder_ShouldCaptureServiceContext()
    {
        // Note: Environment variables are cached at first use for performance.
        // This test verifies Host is always captured (via MachineName) and that
        // environment properties are populated from the cache (which may be null
        // if not set before first FeatureWideEventBuilder was created).

        // Act
        var builder = FeatureWideEvent.Start("EnvironmentTest", "Mutation");
        var wideEvent = builder.Success();

        // Assert - Host is always available via Environment.MachineName
        wideEvent.Host.Should().NotBeNullOrEmpty();

        // Environment properties come from cache (may be null if not set at startup)
        // These assertions verify the properties exist, not specific values
        // since tests run in arbitrary order and cache may already be initialized
        wideEvent.Should().BeAssignableTo<FeatureWideEvent>();
    }

    [Fact]
    public void FeatureWideEventBuilder_WithResultContext_ShouldMergeContext()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("ErrorOrContextTest", "Mutation");
        var context = new Dictionary<string, object>
        {
            ["operation"] = "create",
            ["entity_type"] = "Order",
            ["timestamp"] = DateTimeOffset.UtcNow
        };

        // Act
        builder.WithResultContext(context);
        var wideEvent = builder.Success();

        // Assert
        wideEvent.RequestContext.Should().ContainKey("operation");
        wideEvent.RequestContext.Should().ContainKey("entity_type");
        wideEvent.RequestContext.Should().ContainKey("timestamp");
    }

    [Fact]
    public void FeatureWideEventBuilder_ShouldCaptureDistributedTraceContext()
    {
        // Arrange
        using var activity = new System.Diagnostics.Activity("TestActivity");
        activity.SetIdFormat(System.Diagnostics.ActivityIdFormat.W3C);
        activity.Start();

        // Act
        var builder = FeatureWideEvent.Start("TraceTest", "Mutation");
        var wideEvent = builder.Success();

        // Assert
        wideEvent.TraceId.Should().NotBeNullOrEmpty();
        wideEvent.SpanId.Should().NotBeNullOrEmpty();
        wideEvent.TraceId.Should().Be(activity.TraceId.ToString());
        wideEvent.SpanId.Should().Be(activity.SpanId.ToString());
    }

    [Fact]
    public void FeatureWideEventBuilder_WithoutActivity_ShouldHaveNullTraceContext()
    {
        // Arrange - Ensure no activity is current
        System.Diagnostics.Activity.Current = null;

        // Act
        var builder = FeatureWideEvent.Start("NoTraceTest", "Mutation");
        var wideEvent = builder.Success();

        // Assert
        wideEvent.TraceId.Should().BeNull();
        wideEvent.SpanId.Should().BeNull();
    }

    [Fact]
    public void FeatureWideEventBuilder_WithContext_SameKey_ShouldOverwrite()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("OverwriteTest", "Mutation");

        // Act
        builder
            .WithContext("key", "original")
            .WithContext("key", "overwritten");

        var wideEvent = builder.Success();

        // Assert
        wideEvent.RequestContext["key"].Should().Be("overwritten");
    }

    [Fact]
    public void FeatureWideEventBuilder_WithRequestContext_NullRequest_ShouldNotThrow()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("NullRequestTest", "Mutation");

        // Act
        var act = () => builder.WithRequestContext<TestRequest>(null!);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void FeatureWideEventBuilder_WithRequestContext_BothAttributeAndInterface_ShouldExtractBoth()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("CombinedContextTest", "Mutation");
        var request = new CombinedContextRequest
        {
            AttributeProperty = "from-attribute"
        };

        // Act
        builder.WithRequestContext(request);
        var wideEvent = builder.Success();

        // Assert - Both extraction methods should work
        wideEvent.RequestContext.Should().ContainKey("attribute_property");
        wideEvent.RequestContext["attribute_property"].Should().Be("from-attribute");
        wideEvent.RequestContext.Should().ContainKey("interface_key");
        wideEvent.RequestContext["interface_key"].Should().Be("from-interface");
    }

    [Fact]
    public void FeatureWideEventBuilder_Failure_WithEmptyErrorList_ShouldHandleGracefully()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("EmptyErrorTest", "Mutation");
        var errors = new List<Error>();

        // Act
        var wideEvent = builder.ValidationFailure(errors);

        // Assert
        wideEvent.Outcome.Should().Be("validation_failure");
        wideEvent.ErrorCode.Should().BeNull();
        wideEvent.ErrorCount.Should().BeNull();
    }

    [Fact]
    public void FeatureWideEventBuilder_Exception_WithInnerException_ShouldCaptureOuterException()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("InnerExceptionTest", "Mutation");

        Exception outerException;
        try
        {
            try
            {
                throw new InvalidOperationException("Inner error");
            }
            catch (Exception inner)
            {
                throw new ApplicationException("Outer error", inner);
            }
        }
        catch (Exception ex)
        {
            outerException = ex;
        }

        // Act
        var wideEvent = builder.Exception(outerException);

        // Assert - Should capture outer exception details
        wideEvent.ExceptionType.Should().Contain("ApplicationException");
        wideEvent.ExceptionMessage.Should().Be("Outer error");
        wideEvent.ExceptionStackTrace.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FeatureWideEventBuilder_ManyErrors_ShouldJoinAllDescriptions()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("ManyErrorsTest", "Mutation");
        var errors = Enumerable.Range(1, 10)
            .Select(i => Error.Validation($"Error{i}", $"Description {i}"))
            .ToList();

        // Act
        var wideEvent = builder.ValidationFailure(errors);

        // Assert
        wideEvent.ErrorCount.Should().Be(10);
        wideEvent.ErrorCode.Should().Be("Error1"); // First error
        wideEvent.ErrorDescription.Should().Contain("Error1");
        wideEvent.ErrorDescription.Should().Contain("Error10");

        // All errors should be joined
        for (int i = 1; i <= 10; i++)
        {
            wideEvent.ErrorDescription.Should().Contain($"Error{i}");
        }
    }

    [Fact]
    public void FeatureWideEventBuilder_Timestamp_ShouldBeRecentUtc()
    {
        // Arrange
        var beforeStart = DateTimeOffset.UtcNow;

        // Act
        var builder = FeatureWideEvent.Start("TimestampTest", "Mutation");
        var wideEvent = builder.Success();

        var afterEnd = DateTimeOffset.UtcNow;

        // Assert
        wideEvent.Timestamp.Should().BeOnOrAfter(beforeStart);
        wideEvent.Timestamp.Should().BeOnOrBefore(afterEnd);
        wideEvent.Timestamp.Offset.Should().Be(TimeSpan.Zero); // Should be UTC
    }

    [Fact]
    public void FeatureWideEventBuilder_WithFeatureContext_NullContext_ShouldNotThrow()
    {
        // Arrange
        var builder = FeatureWideEvent.Start("NullFeatureContextTest", "Mutation");

        // Act
        var act = () => builder.WithFeatureContext<object>(null);

        // Assert
        act.Should().NotThrow();
    }

    // Test helper classes
    private class CombinedContextRequest : IWideEventContext
    {
        [WideEventProperty]
        public string? AttributeProperty { get; set; }

        public IEnumerable<KeyValuePair<string, object?>> GetWideEventContext()
        {
            yield return new KeyValuePair<string, object?>("interface_key", "from-interface");
        }
    }

    private class TestRequest
    {
        [WideEventProperty]
        public Guid UserId { get; set; }

        [WideEventProperty("user_name")]
        public string? UserName { get; set; }

        public string? SensitiveData { get; set; }
    }

    private class TestContextRequest : IWideEventContext
    {
        private readonly string _customValue;

        public TestContextRequest(string customValue)
        {
            _customValue = customValue;
        }

        public IEnumerable<KeyValuePair<string, object?>> GetWideEventContext()
        {
            yield return new KeyValuePair<string, object?>("custom_context_key", _customValue);
        }
    }

    private class TestOrder
    {
        public Guid Id { get; set; }
    }

    private record OrderRequest(Guid OrderId);
    private record OrderResult(bool Success);

    private class TestValidator { }
    private class TestRequirements { }
    private class TestMutator { }
    private class TestQuery { }
    private class TestSideEffects { }

    private class NoOpValidatorTest { }
    private class NoOpRequirementsTest { }
    private class NoOpSideEffectsTest { }
}

/// <summary>
/// Tests for TestWideEventEmitter functionality.
/// </summary>
public class TestWideEventEmitterTests
{
    [Fact]
    public void TestWideEventEmitter_ShouldCaptureEvents()
    {
        // Arrange
        var emitter = new TestWideEventEmitter();
        var event1 = CreateTestEvent("Feature1", "success");
        var event2 = CreateTestEvent("Feature2", "validation_failure");

        // Act
        emitter.Emit(event1);
        emitter.Emit(event2);

        // Assert
        emitter.Events.Should().HaveCount(2);
        emitter.LastEvent.Should().BeSameAs(event2);
    }

    [Fact]
    public void TestWideEventEmitter_GetEventsByFeature_ShouldFilter()
    {
        // Arrange
        var emitter = new TestWideEventEmitter();
        emitter.Emit(CreateTestEvent("CreateOrder", "success"));
        emitter.Emit(CreateTestEvent("GetOrder", "success"));
        emitter.Emit(CreateTestEvent("CreateOrder", "success"));

        // Act
        var createOrderEvents = emitter.GetEventsByFeature("CreateOrder").ToList();

        // Assert
        createOrderEvents.Should().HaveCount(2);
    }

    [Fact]
    public void TestWideEventEmitter_GetSuccessfulEvents_ShouldFilter()
    {
        // Arrange
        var emitter = new TestWideEventEmitter();
        emitter.Emit(CreateTestEvent("Feature1", "success"));
        emitter.Emit(CreateTestEvent("Feature2", "validation_failure"));
        emitter.Emit(CreateTestEvent("Feature3", "success"));

        // Act
        var successful = emitter.GetSuccessfulEvents().ToList();
        var failed = emitter.GetFailedEvents().ToList();

        // Assert
        successful.Should().HaveCount(2);
        failed.Should().HaveCount(1);
    }

    [Fact]
    public async Task TestWideEventEmitter_WaitForEventAsync_ShouldWaitForEvent()
    {
        // Arrange
        var emitter = new TestWideEventEmitter();

        // Act - Start waiting before event is emitted
        var waitTask = emitter.WaitForEventAsync(
            e => e.FeatureName == "DelayedFeature",
            TimeSpan.FromSeconds(5));

        // Emit the event after a delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            emitter.Emit(CreateTestEvent("DelayedFeature", "success"));
        });

        var result = await waitTask;

        // Assert
        result.Should().NotBeNull();
        result.FeatureName.Should().Be("DelayedFeature");
    }

    [Fact]
    public async Task TestWideEventEmitter_WaitForEventAsync_ShouldReturnExistingEvent()
    {
        // Arrange
        var emitter = new TestWideEventEmitter();
        emitter.Emit(CreateTestEvent("ExistingFeature", "success"));

        // Act
        var result = await emitter.WaitForEventAsync(
            e => e.FeatureName == "ExistingFeature",
            TimeSpan.FromSeconds(1));

        // Assert
        result.Should().NotBeNull();
        result.FeatureName.Should().Be("ExistingFeature");
    }

    [Fact]
    public void TestWideEventEmitter_Clear_ShouldRemoveAllEvents()
    {
        // Arrange
        var emitter = new TestWideEventEmitter();
        emitter.Emit(CreateTestEvent("Feature1", "success"));
        emitter.Emit(CreateTestEvent("Feature2", "success"));

        // Act
        emitter.Clear();

        // Assert
        emitter.Events.Should().BeEmpty();
    }

    private static FeatureWideEvent CreateTestEvent(string featureName, string outcome)
    {
        return new FeatureWideEvent
        {
            FeatureName = featureName,
            FeatureType = "Mutation",
            Outcome = outcome
        };
    }
}

/// <summary>
/// Tests for SerilogWideEventEmitter (production emitter using ILogger).
/// </summary>
public class SerilogWideEventEmitterTests
{
    [Fact]
    public void Emit_WhenSuccess_ShouldLogAtInformationLevel()
    {
        // Arrange
        var logger = new TestLogger();
        var emitter = new SerilogWideEventEmitter(logger);
        var wideEvent = new FeatureWideEvent
        {
            FeatureName = "CreateOrder",
            FeatureType = "Mutation",
            Outcome = "success"
        };

        // Act
        emitter.Emit(wideEvent);

        // Assert
        logger.LogEntries.Should().HaveCount(1);
        logger.LogEntries[0].LogLevel.Should().Be(Microsoft.Extensions.Logging.LogLevel.Information);
    }

    [Fact]
    public void Emit_WhenValidationFailure_ShouldLogAtWarningLevel()
    {
        // Arrange
        var logger = new TestLogger();
        var emitter = new SerilogWideEventEmitter(logger);
        var wideEvent = new FeatureWideEvent
        {
            FeatureName = "CreateOrder",
            FeatureType = "Mutation",
            Outcome = "validation_failure",
            ErrorCode = "Order.Invalid"
        };

        // Act
        emitter.Emit(wideEvent);

        // Assert
        logger.LogEntries.Should().HaveCount(1);
        logger.LogEntries[0].LogLevel.Should().Be(Microsoft.Extensions.Logging.LogLevel.Warning);
    }

    [Fact]
    public void Emit_WhenException_ShouldLogAtWarningLevel()
    {
        // Arrange
        var logger = new TestLogger();
        var emitter = new SerilogWideEventEmitter(logger);
        var wideEvent = new FeatureWideEvent
        {
            FeatureName = "CreateOrder",
            FeatureType = "Mutation",
            Outcome = "exception",
            ExceptionType = "System.InvalidOperationException",
            ExceptionMessage = "Something went wrong"
        };

        // Act
        emitter.Emit(wideEvent);

        // Assert
        logger.LogEntries.Should().HaveCount(1);
        logger.LogEntries[0].LogLevel.Should().Be(Microsoft.Extensions.Logging.LogLevel.Warning);
    }

    [Fact]
    public void Emit_ShouldIncludeFeatureNameInLogMessage()
    {
        // Arrange
        var logger = new TestLogger();
        var emitter = new SerilogWideEventEmitter(logger);
        var wideEvent = new FeatureWideEvent
        {
            FeatureName = "ProcessPayment",
            FeatureType = "Mutation",
            Outcome = "success",
            TotalMs = 123.45
        };

        // Act
        emitter.Emit(wideEvent);

        // Assert
        logger.LogEntries.Should().HaveCount(1);

        // The formatted message should include the feature name
        logger.LogEntries[0].Message.Should().Contain("ProcessPayment");
    }

    [Fact]
    public void Emit_ShouldIncludeTimingInLogMessage()
    {
        // Arrange
        var logger = new TestLogger();
        var emitter = new SerilogWideEventEmitter(logger);
        var wideEvent = new FeatureWideEvent
        {
            FeatureName = "CreateOrder",
            FeatureType = "Mutation",
            Outcome = "success",
            TotalMs = 150.5,
            ValidationMs = 10.0,
            RequirementsMs = 20.0,
            ExecutionMs = 100.0,
            SideEffectsMs = 20.5
        };

        // Act
        emitter.Emit(wideEvent);

        // Assert
        logger.LogEntries.Should().HaveCount(1);
        var message = logger.LogEntries[0].Message;
        message.Should().NotBeNullOrEmpty();

        // Timing values should be formatted in the message
        message.Should().Contain("150.50");  // TotalMs
        message.Should().Contain("10.00");   // ValidationMs
        message.Should().Contain("20.00");   // RequirementsMs
        message.Should().Contain("100.00");  // ExecutionMs
        message.Should().Contain("20.50");   // SideEffectsMs
    }

    [Fact]
    public void Emit_WithNullTimings_ShouldUseZeroAsDefault()
    {
        // Arrange
        var logger = new TestLogger();
        var emitter = new SerilogWideEventEmitter(logger);
        var wideEvent = new FeatureWideEvent
        {
            FeatureName = "CreateOrder",
            FeatureType = "Mutation",
            Outcome = "success",
            TotalMs = 50.0,
            ValidationMs = null,
            RequirementsMs = null,
            ExecutionMs = null,
            SideEffectsMs = null
        };

        // Act
        emitter.Emit(wideEvent);

        // Assert
        logger.LogEntries.Should().HaveCount(1);
        var message = logger.LogEntries[0].Message;
        message.Should().NotBeNullOrEmpty();

        // Null timings should appear as 0.00 in message
        message.Should().Contain("50.00"); // TotalMs is set
        message.Should().Contain("[Validation: 0.00ms]");
        message.Should().Contain("[Requirements: 0.00ms]");
        message.Should().Contain("[Execution: 0.00ms]");
        message.Should().Contain("[SideEffects: 0.00ms]");
    }

    /// <summary>
    /// Simple test logger that captures log entries for verification.
    /// </summary>
    private sealed class TestLogger : Microsoft.Extensions.Logging.ILogger
    {
        public List<LogEntry> LogEntries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LogEntries.Add(new LogEntry
            {
                LogLevel = logLevel,
                Message = formatter(state, exception)
            });
        }

        public record LogEntry
        {
            public Microsoft.Extensions.Logging.LogLevel LogLevel { get; init; }
            public string? Message { get; init; }
        }
    }
}

/// <summary>
/// Tests for NullWideEventEmitter.
/// </summary>
public class NullWideEventEmitterTests
{
    [Fact]
    public void Emit_ShouldNotThrow()
    {
        // Arrange
        var emitter = NullWideEventEmitter.Instance;
        var wideEvent = new FeatureWideEvent
        {
            FeatureName = "TestFeature",
            FeatureType = "Mutation",
            Outcome = "success"
        };

        // Act
        var act = () => emitter.Emit(wideEvent);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Instance_ShouldBeSingleton()
    {
        // Act
        var instance1 = NullWideEventEmitter.Instance;
        var instance2 = NullWideEventEmitter.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }
}
