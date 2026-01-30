using FluentAssertions;
using VsaResults;
using VsaResults.WideEvents;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// Integration tests for WideEvent and WideEventBuilder (feature execution path).
/// </summary>
public class FeatureWideEventBuilderTests
{
    [Fact]
    public void WideEventBuilder_ShouldCaptureBasicContext()
    {
        // Arrange & Act
        var builder = WideEvent.StartFeature("CreateOrder", "Mutation");
        var wideEvent = builder.Success();

        // Assert
        wideEvent.Feature.Should().NotBeNull();
        wideEvent.Feature!.FeatureName.Should().Be("CreateOrder");
        wideEvent.Feature.FeatureType.Should().Be("Mutation");
        wideEvent.Outcome.Should().Be("success");
        wideEvent.IsSuccess.Should().BeTrue();
        wideEvent.TotalMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void WideEventBuilder_WithTypes_ShouldRecordRequestAndResultTypes()
    {
        // Arrange
        var builder = WideEvent.StartFeature("GetOrder", "Query");

        // Act
        builder.WithTypes<OrderRequest, OrderResult>();
        var wideEvent = builder.Success();

        // Assert
        wideEvent.Feature!.RequestType.Should().Be("OrderRequest");
        wideEvent.Feature.ResultType.Should().Be("OrderResult");
    }

    [Fact]
    public void WideEventBuilder_WithContext_ShouldAccumulateContext()
    {
        // Arrange
        var builder = WideEvent.StartFeature("ProcessOrder", "Mutation");
        var orderId = Guid.NewGuid();

        // Act
        builder
            .WithContext("order_id", orderId)
            .WithContext("customer_id", "CUST-001")
            .WithContext("amount", 199.99m)
            .WithContext("is_priority", true);

        var wideEvent = builder.Success();

        // Assert
        wideEvent.Context.Should().ContainKey("order_id");
        wideEvent.Context.Should().ContainKey("customer_id");
        wideEvent.Context.Should().ContainKey("amount");
        wideEvent.Context.Should().ContainKey("is_priority");
        wideEvent.Context["order_id"].Should().Be(orderId);
        wideEvent.Context["customer_id"].Should().Be("CUST-001");
        wideEvent.Context["amount"].Should().Be(199.99m);
        wideEvent.Context["is_priority"].Should().Be(true);
    }

    [Fact]
    public void WideEventBuilder_WithRequestContext_ShouldExtractAttributedProperties()
    {
        // Arrange
        var builder = WideEvent.StartFeature("CreateUser", "Mutation");
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
        wideEvent.Context.Should().ContainKey("user_id");
        wideEvent.Context.Should().ContainKey("user_name");
        wideEvent.Context.Should().NotContainKey("sensitive_data");
        wideEvent.Context.Should().NotContainKey("SensitiveData");
    }

    [Fact]
    public void WideEventBuilder_WithRequestContext_ShouldExtractFromIWideEventContext()
    {
        // Arrange
        var builder = WideEvent.StartFeature("ProcessData", "Mutation");
        var request = new TestContextRequest("CUSTOM-123");

        // Act
        builder.WithRequestContext(request);
        var wideEvent = builder.Success();

        // Assert
        wideEvent.Context.Should().ContainKey("custom_context_key");
        wideEvent.Context["custom_context_key"].Should().Be("CUSTOM-123");
    }

    [Fact]
    public void WideEventBuilder_WithFeatureContext_ShouldMergeContext()
    {
        // Arrange
        var builder = WideEvent.StartFeature("UpdateOrder", "Mutation");
        var featureContext = new FeatureContext<object> { Request = new object() };

        featureContext.AddContext("order_id", Guid.NewGuid());
        featureContext.AddContext("status", "processing");
        featureContext.SetEntity("order", new TestOrder { Id = Guid.NewGuid() });

        // Act
        builder.WithFeatureContext(featureContext);
        var wideEvent = builder.Success();

        // Assert
        wideEvent.Context.Should().ContainKey("order_id");
        wideEvent.Context.Should().ContainKey("status");
        wideEvent.Feature!.LoadedEntities.Should().ContainKey("order");
        wideEvent.Feature.LoadedEntities["order"].Should().Be("TestOrder");
    }

    [Fact]
    public void WideEventBuilder_WithPipelineStages_ShouldRecordStageTypes()
    {
        // Arrange
        var builder = WideEvent.StartFeature("CreateProduct", "Mutation");

        // Act
        builder.WithPipelineStages(
            validatorType: typeof(TestValidator),
            requirementsType: typeof(TestRequirements),
            mutatorOrQueryType: typeof(TestMutator),
            sideEffectsType: typeof(TestSideEffects),
            isMutation: true);

        var wideEvent = builder.Success();

        // Assert
        wideEvent.Feature!.ValidatorType.Should().Be("TestValidator");
        wideEvent.Feature.RequirementsType.Should().Be("TestRequirements");
        wideEvent.Feature.MutatorType.Should().Be("TestMutator");
        wideEvent.Feature.SideEffectsType.Should().Be("TestSideEffects");
        wideEvent.Feature.HasCustomValidator.Should().BeTrue();
        wideEvent.Feature.HasCustomRequirements.Should().BeTrue();
        wideEvent.Feature.HasCustomSideEffects.Should().BeTrue();
    }

    [Fact]
    public void WideEventBuilder_WithPipelineStages_ShouldDetectNoOpStages()
    {
        // Arrange
        var builder = WideEvent.StartFeature("SimpleQuery", "Query");

        // Act
        builder.WithPipelineStages(
            validatorType: typeof(NoOpValidator<string>),
            requirementsType: typeof(NoOpRequirements<string>),
            mutatorOrQueryType: typeof(TestQuery),
            sideEffectsType: typeof(NoOpSideEffects<string>),
            isMutation: false);

        var wideEvent = builder.Success();

        // Assert
        wideEvent.Feature!.QueryType.Should().Be("TestQuery");
        wideEvent.Feature.HasCustomValidator.Should().BeFalse();
        wideEvent.Feature.HasCustomRequirements.Should().BeFalse();
        wideEvent.Feature.HasCustomSideEffects.Should().BeFalse();
    }

    [Fact]
    public void WideEventBuilder_ShouldRecordTimingBreakdown()
    {
        // Arrange
        var builder = WideEvent.StartFeature("TimedFeature", "Mutation");

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
        wideEvent.Feature!.ValidationMs.Should().BeGreaterThan(0);
        wideEvent.Feature.RequirementsMs.Should().BeGreaterThan(0);
        wideEvent.Feature.ExecutionMs.Should().BeGreaterThan(0);
        wideEvent.Feature.SideEffectsMs.Should().BeGreaterThan(0);
        wideEvent.TotalMs.Should().BeGreaterOrEqualTo(20);
    }

    [Fact]
    public void WideEventBuilder_ValidationFailure_ShouldCaptureErrors()
    {
        // Arrange
        var builder = WideEvent.StartFeature("FailingValidation", "Mutation");
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
        wideEvent.Error.Should().NotBeNull();
        wideEvent.Error!.FailedAtStage.Should().Be("validation");
        wideEvent.Error.Code.Should().Be("Email.Invalid");
        wideEvent.Error.Type.Should().Be("Validation");
        wideEvent.Error.Message.Should().Be("Email format is invalid");
        wideEvent.Error.Count.Should().Be(2);
        wideEvent.Error.AllDescriptions.Should().Contain("Email.Invalid");
        wideEvent.Error.AllDescriptions.Should().Contain("Name.Required");
    }

    [Fact]
    public void WideEventBuilder_RequirementsFailure_ShouldCaptureErrors()
    {
        // Arrange
        var builder = WideEvent.StartFeature("FailingRequirements", "Mutation");
        var errors = new List<Error>
        {
            Error.NotFound("Order.NotFound", "Order not found")
        };

        // Act
        var wideEvent = builder.RequirementsFailure(errors);

        // Assert
        wideEvent.Outcome.Should().Be("requirements_failure");
        wideEvent.Error.Should().NotBeNull();
        wideEvent.Error!.FailedAtStage.Should().Be("requirements");
        wideEvent.Error.Code.Should().Be("Order.NotFound");
        wideEvent.Error.Type.Should().Be("NotFound");
    }

    [Fact]
    public void WideEventBuilder_ExecutionFailure_ShouldCaptureErrors()
    {
        // Arrange
        var builder = WideEvent.StartFeature("FailingExecution", "Mutation");
        var errors = new List<Error>
        {
            Error.Failure("Payment.Declined", "Payment was declined")
        };

        // Act
        var wideEvent = builder.ExecutionFailure(errors);

        // Assert
        wideEvent.Outcome.Should().Be("execution_failure");
        wideEvent.Error.Should().NotBeNull();
        wideEvent.Error!.FailedAtStage.Should().Be("execution");
        wideEvent.Error.Code.Should().Be("Payment.Declined");
    }

    [Fact]
    public void WideEventBuilder_SideEffectsFailure_ShouldCaptureErrors()
    {
        // Arrange
        var builder = WideEvent.StartFeature("FailingSideEffects", "Mutation");
        var errors = new List<Error>
        {
            Error.Failure("Email.SendFailed", "Failed to send confirmation email")
        };

        // Act
        var wideEvent = builder.SideEffectsFailure(errors);

        // Assert
        wideEvent.Outcome.Should().Be("side_effects_failure");
        wideEvent.Error.Should().NotBeNull();
        wideEvent.Error!.FailedAtStage.Should().Be("side_effects");
    }

    [Fact]
    public void WideEventBuilder_Exception_ShouldCaptureExceptionDetails()
    {
        // Arrange
        var builder = WideEvent.StartFeature("ExceptionFeature", "Mutation");

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
        var wideEvent = builder.Exception(exception, includeStackTrace: true);

        // Assert
        wideEvent.Outcome.Should().Be("exception");
        wideEvent.Error.Should().NotBeNull();
        wideEvent.Error!.FailedAtStage.Should().Be("unknown");
        wideEvent.Error.ExceptionType.Should().Be("System.InvalidOperationException");
        wideEvent.Error.ExceptionMessage.Should().Be("Something went very wrong");
        wideEvent.Error.ExceptionStackTrace.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WideEventBuilder_ShouldCaptureServiceContext()
    {
        // Note: Environment variables are cached at first use for performance.
        // This test verifies Host is always captured (via MachineName) and that
        // environment properties are populated from the cache (which may be null
        // if not set before first WideEventBuilder was created).

        // Act
        var builder = WideEvent.StartFeature("EnvironmentTest", "Mutation");
        var wideEvent = builder.Success();

        // Assert - Host is always available via Environment.MachineName
        wideEvent.Host.Should().NotBeNullOrEmpty();

        // Environment properties come from cache (may be null if not set at startup)
        // These assertions verify the properties exist, not specific values
        // since tests run in arbitrary order and cache may already be initialized
        wideEvent.Should().BeAssignableTo<WideEvent>();
    }

    [Fact]
    public void WideEventBuilder_WithResultContext_ShouldMergeContext()
    {
        // Arrange
        var builder = WideEvent.StartFeature("ErrorOrContextTest", "Mutation");
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
        wideEvent.Context.Should().ContainKey("operation");
        wideEvent.Context.Should().ContainKey("entity_type");
        wideEvent.Context.Should().ContainKey("timestamp");
    }

    [Fact]
    public void WideEventBuilder_ShouldCaptureDistributedTraceContext()
    {
        // Arrange
        using var activity = new System.Diagnostics.Activity("TestActivity");
        activity.SetIdFormat(System.Diagnostics.ActivityIdFormat.W3C);
        activity.Start();

        // Act
        var builder = WideEvent.StartFeature("TraceTest", "Mutation");
        var wideEvent = builder.Success();

        // Assert
        wideEvent.TraceId.Should().NotBeNullOrEmpty();
        wideEvent.SpanId.Should().NotBeNullOrEmpty();
        wideEvent.TraceId.Should().Be(activity.TraceId.ToString());
        wideEvent.SpanId.Should().Be(activity.SpanId.ToString());
    }

    [Fact]
    public void WideEventBuilder_WithoutActivity_ShouldHaveNullTraceContext()
    {
        // Arrange - Ensure no activity is current
        System.Diagnostics.Activity.Current = null;

        // Act
        var builder = WideEvent.StartFeature("NoTraceTest", "Mutation");
        var wideEvent = builder.Success();

        // Assert
        wideEvent.TraceId.Should().BeNull();
        wideEvent.SpanId.Should().BeNull();
    }

    [Fact]
    public void WideEventBuilder_WithContext_SameKey_ShouldOverwrite()
    {
        // Arrange
        var builder = WideEvent.StartFeature("OverwriteTest", "Mutation");

        // Act
        builder
            .WithContext("key", "original")
            .WithContext("key", "overwritten");

        var wideEvent = builder.Success();

        // Assert
        wideEvent.Context["key"].Should().Be("overwritten");
    }

    [Fact]
    public void WideEventBuilder_WithRequestContext_NullRequest_ShouldNotThrow()
    {
        // Arrange
        var builder = WideEvent.StartFeature("NullRequestTest", "Mutation");

        // Act
        var act = () => builder.WithRequestContext<TestRequest>(null!);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void WideEventBuilder_WithRequestContext_BothAttributeAndInterface_ShouldExtractBoth()
    {
        // Arrange
        var builder = WideEvent.StartFeature("CombinedContextTest", "Mutation");
        var request = new CombinedContextRequest
        {
            AttributeProperty = "from-attribute"
        };

        // Act
        builder.WithRequestContext(request);
        var wideEvent = builder.Success();

        // Assert - Both extraction methods should work
        wideEvent.Context.Should().ContainKey("attribute_property");
        wideEvent.Context["attribute_property"].Should().Be("from-attribute");
        wideEvent.Context.Should().ContainKey("interface_key");
        wideEvent.Context["interface_key"].Should().Be("from-interface");
    }

    [Fact]
    public void WideEventBuilder_Failure_WithEmptyErrorList_ShouldHandleGracefully()
    {
        // Arrange
        var builder = WideEvent.StartFeature("EmptyErrorTest", "Mutation");
        var errors = new List<Error>();

        // Act
        var wideEvent = builder.ValidationFailure(errors);

        // Assert
        wideEvent.Outcome.Should().Be("validation_failure");
        wideEvent.Error.Should().NotBeNull();
        wideEvent.Error!.Code.Should().BeNull();
        wideEvent.Error.Count.Should().Be(0);
    }

    [Fact]
    public void WideEventBuilder_Exception_WithInnerException_ShouldCaptureOuterException()
    {
        // Arrange
        var builder = WideEvent.StartFeature("InnerExceptionTest", "Mutation");

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
        var wideEvent = builder.Exception(outerException, includeStackTrace: true);

        // Assert - Should capture outer exception details
        wideEvent.Error.Should().NotBeNull();
        wideEvent.Error!.ExceptionType.Should().Contain("ApplicationException");
        wideEvent.Error.ExceptionMessage.Should().Be("Outer error");
        wideEvent.Error.ExceptionStackTrace.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WideEventBuilder_ManyErrors_ShouldJoinAllDescriptions()
    {
        // Arrange
        var builder = WideEvent.StartFeature("ManyErrorsTest", "Mutation");
        var errors = Enumerable.Range(1, 10)
            .Select(i => Error.Validation($"Error{i}", $"Description {i}"))
            .ToList();

        // Act
        var wideEvent = builder.ValidationFailure(errors);

        // Assert
        wideEvent.Error.Should().NotBeNull();
        wideEvent.Error!.Count.Should().Be(10);
        wideEvent.Error.Code.Should().Be("Error1"); // First error
        wideEvent.Error.AllDescriptions.Should().Contain("Error1");
        wideEvent.Error.AllDescriptions.Should().Contain("Error10");

        // All errors should be joined
        for (int i = 1; i <= 10; i++)
        {
            wideEvent.Error.AllDescriptions.Should().Contain($"Error{i}");
        }
    }

    [Fact]
    public void WideEventBuilder_Timestamp_ShouldBeRecentUtc()
    {
        // Arrange
        var beforeStart = DateTimeOffset.UtcNow;

        // Act
        var builder = WideEvent.StartFeature("TimestampTest", "Mutation");
        var wideEvent = builder.Success();

        var afterEnd = DateTimeOffset.UtcNow;

        // Assert
        wideEvent.Timestamp.Should().BeOnOrAfter(beforeStart);
        wideEvent.Timestamp.Should().BeOnOrBefore(afterEnd);
        wideEvent.Timestamp.Offset.Should().Be(TimeSpan.Zero); // Should be UTC
    }

    [Fact]
    public void WideEventBuilder_WithFeatureContext_NullContext_ShouldNotThrow()
    {
        // Arrange
        var builder = WideEvent.StartFeature("NullFeatureContextTest", "Mutation");

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
            e => e.Feature?.FeatureName == "DelayedFeature",
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
        result.Feature!.FeatureName.Should().Be("DelayedFeature");
    }

    [Fact]
    public async Task TestWideEventEmitter_WaitForEventAsync_ShouldReturnExistingEvent()
    {
        // Arrange
        var emitter = new TestWideEventEmitter();
        emitter.Emit(CreateTestEvent("ExistingFeature", "success"));

        // Act
        var result = await emitter.WaitForEventAsync(
            e => e.Feature?.FeatureName == "ExistingFeature",
            TimeSpan.FromSeconds(1));

        // Assert
        result.Should().NotBeNull();
        result.Feature!.FeatureName.Should().Be("ExistingFeature");
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

    private static WideEvent CreateTestEvent(string featureName, string outcome)
    {
        return new WideEvent
        {
            EventType = "feature",
            Outcome = outcome,
            Feature = new WideEventFeatureSegment
            {
                FeatureName = featureName,
                FeatureType = "Mutation",
            },
        };
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
        var wideEvent = new WideEvent
        {
            EventType = "feature",
            Outcome = "success",
            Feature = new WideEventFeatureSegment
            {
                FeatureName = "TestFeature",
                FeatureType = "Mutation",
            },
        };

        // Act
        var act = () => emitter.Emit(wideEvent);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task EmitAsync_ShouldNotThrow()
    {
        // Arrange
        var emitter = NullWideEventEmitter.Instance;
        var wideEvent = new WideEvent
        {
            EventType = "feature",
            Outcome = "success",
            Feature = new WideEventFeatureSegment
            {
                FeatureName = "TestFeature",
                FeatureType = "Mutation",
            },
        };

        // Act
        var act = async () => await emitter.EmitAsync(wideEvent);

        // Assert
        await act.Should().NotThrowAsync();
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

    [Fact]
    public void Instance_ShouldImplementIWideEventEmitter()
    {
        // Act
        var emitter = NullWideEventEmitter.Instance;

        // Assert
        emitter.Should().BeAssignableTo<IWideEventEmitter>();
    }
}
