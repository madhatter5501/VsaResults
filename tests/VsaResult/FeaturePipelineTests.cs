using FluentAssertions;
using VsaResults;
using VsaResults.WideEvents;

namespace Tests;

public class FeaturePipelineTests
{
    // Test feature implementations
    public sealed record TestRequest(string Value);

    public sealed record TestResult(string ProcessedValue);

    // A simple mutation feature for testing
    public sealed class TestMutationFeature : IMutationFeature<TestRequest, TestResult>
    {
        public IFeatureValidator<TestRequest> Validator { get; set; } = NoOpValidator<TestRequest>.Instance;

        public IFeatureRequirements<TestRequest> Requirements { get; set; } = NoOpRequirements<TestRequest>.Instance;

        public IFeatureMutator<TestRequest, TestResult> Mutator { get; set; } = new TestMutator();

        public IFeatureSideEffects<TestRequest> SideEffects { get; set; } = NoOpSideEffects<TestRequest>.Instance;
    }

    public sealed class TestMutator : IFeatureMutator<TestRequest, TestResult>
    {
        public Task<VsaResult<TestResult>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            return Task.FromResult<VsaResult<TestResult>>(new TestResult($"Processed: {context.Request.Value}"));
        }
    }

    // A simple query feature for testing
    public sealed class TestQueryFeature : IQueryFeature<TestRequest, TestResult>
    {
        public IFeatureValidator<TestRequest> Validator { get; set; } = NoOpValidator<TestRequest>.Instance;

        public IFeatureQuery<TestRequest, TestResult> Query { get; set; } = new TestQuery();
    }

    public sealed class TestQuery : IFeatureQuery<TestRequest, TestResult>
    {
        public Task<VsaResult<TestResult>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            return Task.FromResult<VsaResult<TestResult>>(new TestResult($"Queried: {context.Request.Value}"));
        }
    }

    // Test implementations for failure scenarios
    public sealed class FailingValidator : IFeatureValidator<TestRequest>
    {
        public Task<VsaResult<TestRequest>> ValidateAsync(TestRequest request, CancellationToken ct = default)
        {
            return Task.FromResult<VsaResult<TestRequest>>(Error.Validation("Test.ValidationFailed", "Validation failed"));
        }
    }

    public sealed class CancelingValidator : IFeatureValidator<TestRequest>
    {
        public Task<VsaResult<TestRequest>> ValidateAsync(TestRequest request, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<VsaResult<TestRequest>>(request);
        }
    }

    public sealed class FailingRequirements : IFeatureRequirements<TestRequest>
    {
        public Task<VsaResult<FeatureContext<TestRequest>>> EnforceAsync(TestRequest request, CancellationToken ct = default)
        {
            return Task.FromResult<VsaResult<FeatureContext<TestRequest>>>(Error.NotFound("Test.NotFound", "Entity not found"));
        }
    }

    public sealed class FailingMutator : IFeatureMutator<TestRequest, TestResult>
    {
        public Task<VsaResult<TestResult>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            return Task.FromResult<VsaResult<TestResult>>(Error.Failure("Test.ExecutionFailed", "Execution failed"));
        }
    }

    // Capture emitter for testing
    public sealed class CaptureWideEventEmitter : IWideEventEmitter
    {
        public WideEvent? LastEvent { get; private set; }

        public ValueTask EmitAsync(WideEvent wideEvent, CancellationToken ct = default)
        {
            LastEvent = wideEvent;
            return ValueTask.CompletedTask;
        }

        public void Emit(WideEvent wideEvent)
        {
            LastEvent = wideEvent;
        }
    }

    [Fact]

    public async Task MutationFeature_WhenAllStagesSucceed_ReturnsResult()
    {
        // Arrange
        var feature = new TestMutationFeature();
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        var result = await feature.ExecuteAsync(request, emitter);

        // Assert
        result.ShouldBeSuccess();
        result.Value.ProcessedValue.Should().Be("Processed: test");
        emitter.LastEvent.Should().NotBeNull();

        emitter.LastEvent!.Outcome.Should().Be("success");
        emitter.LastEvent.Feature!.FeatureType.Should().Be("Mutation");
    }

    [Fact]
    public async Task MutationFeature_WhenValidationFails_ReturnsError()
    {
        // Arrange
        var feature = new TestMutationFeature { Validator = new FailingValidator() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        var result = await feature.ExecuteAsync(request, emitter);

        // Assert
        result.ShouldBeValidationError("Test.ValidationFailed");
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Outcome.Should().Be("validation_failure");
        emitter.LastEvent.Error!.FailedAtStage.Should().Be("validation");
    }

    [Fact]

    public async Task QueryFeature_WhenValidationFails_ReturnsError()
    {
        // Arrange
        var feature = new TestQueryFeature { Validator = new FailingValidator() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        var result = await feature.ExecuteAsync(request, emitter);

        // Assert
        result.ShouldBeValidationError("Test.ValidationFailed");
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Outcome.Should().Be("validation_failure");
    }

    [Fact]
    public async Task Feature_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var feature = new TestMutationFeature { Validator = new CancelingValidator() };
        var request = new TestRequest("test");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var action = async () => await feature.ExecuteAsync(request, emitter: (IWideEventEmitter?)null, ct: cts.Token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Feature_WhenNoEmitterProvided_StillExecutes()
    {
        // Arrange
        var feature = new TestMutationFeature();
        var request = new TestRequest("test");

        // Act
        var result = await feature.ExecuteAsync(request);

        // Assert
        result.ShouldBeSuccess();
        result.Value.ProcessedValue.Should().Be("Processed: test");
    }

    [Fact]

    public async Task WideEvent_RecordsTimings()
    {
        // Arrange
        var feature = new TestMutationFeature();
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        await feature.ExecuteAsync(request, emitter);

        // Assert
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.TotalMs.Should().BeGreaterOrEqualTo(0);
        emitter.LastEvent.Feature!.ValidationMs.Should().BeGreaterOrEqualTo(0);
        emitter.LastEvent.Feature.RequirementsMs.Should().BeGreaterOrEqualTo(0);
        emitter.LastEvent.Feature.ExecutionMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task WideEvent_RecordsTypeInformation()
    {
        // Arrange
        var feature = new TestMutationFeature();
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        await feature.ExecuteAsync(request, emitter);

        // Assert
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Feature!.RequestType.Should().Be(nameof(TestRequest));
        emitter.LastEvent.Feature.ResultType.Should().Be(nameof(TestResult));
    }

    // ==================== Side Effects Tests ====================
    public sealed class FailingSideEffects : IFeatureSideEffects<TestRequest>
    {
        public Task<VsaResult<Unit>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            return Task.FromResult<VsaResult<Unit>>(Error.Failure("SideEffects.Failed", "Side effects failed"));
        }
    }

    [Fact]
    public async Task MutationFeature_WhenSideEffectsFail_ReturnsError()
    {
        // Arrange
        var feature = new TestMutationFeature { SideEffects = new FailingSideEffects() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        var result = await feature.ExecuteAsync(request, emitter);

        // Assert
        result.ShouldBeError("SideEffects.Failed", ErrorType.Failure);
        emitter.LastEvent.Should().NotBeNull();

        emitter.LastEvent!.Outcome.Should().Be("side_effects_failure");
        emitter.LastEvent.Error!.FailedAtStage.Should().Be("side_effects");
    }

    [Fact]
    public async Task MutationFeature_WhenSideEffectsSucceed_RecordsSideEffectsTiming()
    {
        // Arrange
        var feature = new TestMutationFeature { SideEffects = new DelayedSideEffects() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        await feature.ExecuteAsync(request, emitter);

        // Assert
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Outcome.Should().Be("success");
        emitter.LastEvent.Feature!.SideEffectsMs.Should().BeGreaterOrEqualTo(0);
    }

    public sealed class DelayedSideEffects : IFeatureSideEffects<TestRequest>
    {
        public async Task<VsaResult<Unit>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            await Task.Delay(1, ct);
            return Unit.Value;
        }
    }

    // ==================== Context Propagation Tests ====================
    public sealed class ContextCapturingMutator : IFeatureMutator<TestRequest, TestResult>
    {
        public Dictionary<string, object?>? CapturedContext { get; private set; }
        public Dictionary<string, object>? CapturedEntities { get; private set; }

        public Task<VsaResult<TestResult>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            CapturedContext = new Dictionary<string, object?>(context.WideEventContext);
            CapturedEntities = new Dictionary<string, object>(context.Entities);
            return Task.FromResult<VsaResult<TestResult>>(new TestResult($"Processed: {context.Request.Value}"));
        }
    }

    public sealed class ContextAddingRequirements : IFeatureRequirements<TestRequest>
    {
        public Task<VsaResult<FeatureContext<TestRequest>>> EnforceAsync(TestRequest request, CancellationToken ct = default)
        {
            var context = new FeatureContext<TestRequest> { Request = request };
            context.SetEntity("user", new { Id = 123, Name = "Test User" });
            context.AddContext("user_id", 123);
            context.AddContext("tenant_id", "tenant-abc");
            return Task.FromResult<VsaResult<FeatureContext<TestRequest>>>(context);
        }
    }

    [Fact]
    public async Task MutationFeature_ContextFlowsThroughPipeline()
    {
        // Arrange
        var mutator = new ContextCapturingMutator();
        var feature = new TestMutationFeature
        {
            Requirements = new ContextAddingRequirements(),
            Mutator = mutator,
        };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        await feature.ExecuteAsync(request, emitter);

        // Assert - Context propagated to mutator
        mutator.CapturedContext.Should().NotBeNull();
        mutator.CapturedContext!["user_id"].Should().Be(123);
        mutator.CapturedContext["tenant_id"].Should().Be("tenant-abc");

        // Assert - Entities propagated to mutator
        mutator.CapturedEntities.Should().NotBeNull();
        mutator.CapturedEntities!.Should().ContainKey("user");

        // Assert - Context captured in wide event
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Context.Should().ContainKey("user_id");
        emitter.LastEvent.Context["user_id"].Should().Be(123);
        emitter.LastEvent.Feature!.LoadedEntities.Should().ContainKey("user");
    }

    [Fact]
    public async Task WideEvent_RecordsErrorDetails()
    {
        // Arrange
        var feature = new TestMutationFeature { Validator = new FailingValidator() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        await feature.ExecuteAsync(request, emitter);

        // Assert
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Error.Should().NotBeNull();
        emitter.LastEvent.Error!.Code.Should().Be("Test.ValidationFailed");
        emitter.LastEvent.Error.Message.Should().Be("Validation failed");
        emitter.LastEvent.Error.Type.Should().Be("Validation");
        emitter.LastEvent.Error.Count.Should().Be(1);
    }

    // ==================== Exception Handling Tests ====================
    public sealed class ThrowingValidator : IFeatureValidator<TestRequest>
    {
        public Task<VsaResult<TestRequest>> ValidateAsync(TestRequest request, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Validator exploded");
        }
    }

    public sealed class ThrowingRequirements : IFeatureRequirements<TestRequest>
    {
        public Task<VsaResult<FeatureContext<TestRequest>>> EnforceAsync(TestRequest request, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Requirements exploded");
        }
    }

    public sealed class ThrowingMutator : IFeatureMutator<TestRequest, TestResult>
    {
        public Task<VsaResult<TestResult>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Mutator exploded");
        }
    }

    public sealed class ThrowingSideEffects : IFeatureSideEffects<TestRequest>
    {
        public Task<VsaResult<Unit>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Side effects exploded");
        }
    }

    [Fact]
    public async Task MutationFeature_WhenValidatorThrows_EmitsExceptionEvent()
    {
        // Arrange
        var feature = new TestMutationFeature { Validator = new ThrowingValidator() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act & Assert
        var act = () => feature.ExecuteAsync(request, emitter);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Validator exploded");

        // Wide event should still be emitted with exception details
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Outcome.Should().Be("exception");
        emitter.LastEvent.Error!.FailedAtStage.Should().Be("validation");
        emitter.LastEvent.Error.FailedInClass.Should().Be("ThrowingValidator");
        emitter.LastEvent.Error.FailedInMethod.Should().Be("ValidateAsync");
        emitter.LastEvent.Error.ExceptionType.Should().Contain("InvalidOperationException");
        emitter.LastEvent.Error.ExceptionMessage.Should().Be("Validator exploded");
    }

    [Fact]
    public async Task MutationFeature_WhenRequirementsThrows_EmitsExceptionEvent()
    {
        // Arrange
        var feature = new TestMutationFeature { Requirements = new ThrowingRequirements() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act & Assert
        var act = () => feature.ExecuteAsync(request, emitter);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Requirements exploded");

        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Outcome.Should().Be("exception");
        emitter.LastEvent.Error!.FailedAtStage.Should().Be("requirements");
        emitter.LastEvent.Error.FailedInClass.Should().Be("ThrowingRequirements");
        emitter.LastEvent.Error.FailedInMethod.Should().Be("EnforceAsync");
        emitter.LastEvent.Error.ExceptionMessage.Should().Be("Requirements exploded");
    }

    [Fact]
    public async Task MutationFeature_WhenMutatorThrows_EmitsExceptionEvent()
    {
        // Arrange
        var feature = new TestMutationFeature { Mutator = new ThrowingMutator() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act & Assert
        var act = () => feature.ExecuteAsync(request, emitter);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Mutator exploded");

        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Outcome.Should().Be("exception");
        emitter.LastEvent.Error!.FailedAtStage.Should().Be("execution");
        emitter.LastEvent.Error.FailedInClass.Should().NotBeNull();
        emitter.LastEvent.Error.FailedInMethod.Should().Be("ExecuteAsync");
        emitter.LastEvent.Error.ExceptionMessage.Should().Be("Mutator exploded");
    }

    [Fact]
    public async Task MutationFeature_WhenSideEffectsThrows_EmitsExceptionEvent()
    {
        // Arrange
        var feature = new TestMutationFeature { SideEffects = new ThrowingSideEffects() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act & Assert
        var act = () => feature.ExecuteAsync(request, emitter);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Side effects exploded");

        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Outcome.Should().Be("exception");
        emitter.LastEvent.Error!.FailedAtStage.Should().Be("side_effects");
        emitter.LastEvent.Error.FailedInClass.Should().NotBeNull();
        emitter.LastEvent.Error.FailedInMethod.Should().Be("ExecuteAsync");
        emitter.LastEvent.Error.ExceptionMessage.Should().Be("Side effects exploded");
    }

    // ==================== Pipeline Stage Type Recording Tests ====================
    [Fact]
    public async Task WideEvent_RecordsPipelineStageTypes()
    {
        // Arrange
        var feature = new TestMutationFeature();
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        await feature.ExecuteAsync(request, emitter);

        // Assert
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Feature!.ValidatorType.Should().NotBeNullOrEmpty();
        emitter.LastEvent.Feature.RequirementsType.Should().NotBeNullOrEmpty();
        emitter.LastEvent.Feature.SideEffectsType.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task WideEvent_DetectsNoOpComponents()
    {
        // Arrange - Using default NoOp implementations
        var feature = new TestMutationFeature();
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        await feature.ExecuteAsync(request, emitter);

        // Assert
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Feature!.HasCustomValidator.Should().BeFalse();
        emitter.LastEvent.Feature.HasCustomRequirements.Should().BeFalse();
        emitter.LastEvent.Feature.HasCustomSideEffects.Should().BeFalse();
    }

    [Fact]
    public async Task WideEvent_DetectsCustomComponents()
    {
        // Arrange - Using custom implementations
        var feature = new TestMutationFeature
        {
            Validator = new FailingValidator(),
            Requirements = new ContextAddingRequirements(),
            SideEffects = new DelayedSideEffects(),
        };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        await feature.ExecuteAsync(request, emitter);

        // Assert
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Feature!.HasCustomValidator.Should().BeTrue();
        emitter.LastEvent.Feature.HasCustomRequirements.Should().BeTrue();
        emitter.LastEvent.Feature.HasCustomSideEffects.Should().BeTrue();
    }

    // ==================== Multiple Errors Tests ====================
    public sealed class MultiErrorValidator : IFeatureValidator<TestRequest>
    {
        public Task<VsaResult<TestRequest>> ValidateAsync(TestRequest request, CancellationToken ct = default)
        {
            var errors = new List<Error>
            {
                Error.Validation("Name.Required", "Name is required"),
                Error.Validation("Email.Invalid", "Email format is invalid"),
                Error.Validation("Age.OutOfRange", "Age must be between 0 and 150"),
            };
            return Task.FromResult<VsaResult<TestRequest>>(errors);
        }
    }

    [Fact]
    public async Task WideEvent_RecordsMultipleErrors()
    {
        // Arrange
        var feature = new TestMutationFeature { Validator = new MultiErrorValidator() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        await feature.ExecuteAsync(request, emitter);

        // Assert
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Error.Should().NotBeNull();
        emitter.LastEvent.Error!.Count.Should().Be(3);
        emitter.LastEvent.Error.Code.Should().Be("Name.Required"); // First error
        emitter.LastEvent.Error.AllDescriptions.Should().Contain("Email.Invalid");
        emitter.LastEvent.Error.AllDescriptions.Should().Contain("Age.OutOfRange");
    }

    // ==================== Query Feature Failure Tests ====================
    public sealed class FailingQuery : IFeatureQuery<TestRequest, TestResult>
    {
        public Task<VsaResult<TestResult>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            return Task.FromResult<VsaResult<TestResult>>(Error.Failure("Query.Failed", "Query execution failed"));
        }
    }

    public sealed class ThrowingQuery : IFeatureQuery<TestRequest, TestResult>
    {
        public Task<VsaResult<TestResult>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Query exploded");
        }
    }

    [Fact]
    public async Task QueryFeature_WhenQueryFails_ReturnsError()
    {
        // Arrange
        var feature = new TestQueryFeature { Query = new FailingQuery() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        var result = await feature.ExecuteAsync(request, emitter);

        // Assert
        result.ShouldBeError("Query.Failed", ErrorType.Failure);
        emitter.LastEvent.Should().NotBeNull();

        emitter.LastEvent!.Outcome.Should().Be("execution_failure");
        emitter.LastEvent.Error!.FailedAtStage.Should().Be("execution");
        emitter.LastEvent.Feature!.FeatureType.Should().Be("Query");
    }

    [Fact]
    public async Task QueryFeature_WhenQueryThrows_EmitsExceptionEvent()
    {
        // Arrange
        var feature = new TestQueryFeature { Query = new ThrowingQuery() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act & Assert
        var act = () => feature.ExecuteAsync(request, emitter);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Query exploded");

        // Wide event should still be emitted with exception details
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Outcome.Should().Be("exception");
        emitter.LastEvent.Error!.FailedAtStage.Should().Be("execution");
        emitter.LastEvent.Error.ExceptionType.Should().Contain("InvalidOperationException");
        emitter.LastEvent.Error.ExceptionMessage.Should().Be("Query exploded");
        emitter.LastEvent.Feature!.FeatureType.Should().Be("Query");
    }

    // ==================== Null Mutator/Query Tests ====================
    public sealed class NullMutatorFeature : IMutationFeature<TestRequest, TestResult>
    {
        public IFeatureValidator<TestRequest> Validator { get; set; } = NoOpValidator<TestRequest>.Instance;
        public IFeatureRequirements<TestRequest> Requirements { get; set; } = NoOpRequirements<TestRequest>.Instance;
        public IFeatureMutator<TestRequest, TestResult> Mutator { get; set; } = null!;
        public IFeatureSideEffects<TestRequest> SideEffects { get; set; } = NoOpSideEffects<TestRequest>.Instance;
    }

    public sealed class NullQueryFeature : IQueryFeature<TestRequest, TestResult>
    {
        public IFeatureValidator<TestRequest> Validator { get; set; } = NoOpValidator<TestRequest>.Instance;
        public IFeatureQuery<TestRequest, TestResult> Query { get; set; } = null!;
    }

    [Fact]
    public async Task MutationFeature_WhenMutatorIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var feature = new NullMutatorFeature();
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act & Assert
        var act = () => feature.ExecuteAsync(request, emitter);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Mutator is required for mutation features.");
    }

    [Fact]
    public async Task QueryFeature_WhenQueryIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var feature = new NullQueryFeature();
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act & Assert
        var act = () => feature.ExecuteAsync(request, emitter);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Query is required for query features.");
    }

    // ==================== Context From Result Tests ====================
    public sealed class ContextAddingMutator : IFeatureMutator<TestRequest, TestResult>
    {
        public Task<VsaResult<TestResult>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            // Return result with context attached via ErrorOr
            var result = new TestResult($"Processed: {context.Request.Value}");
            return Task.FromResult(VsaResultFactory.From(result)
                .WithContext("mutator_added_key", "mutator_value"));
        }
    }

    [Fact]
    public async Task WideEvent_ShouldMergeContextFromMutatorResult()
    {
        // Arrange
        var feature = new TestMutationFeature { Mutator = new ContextAddingMutator() };
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        await feature.ExecuteAsync(request, emitter);

        // Assert
        emitter.LastEvent.Should().NotBeNull();
        emitter.LastEvent!.Outcome.Should().Be("success");
        emitter.LastEvent.Context.Should().ContainKey("mutator_added_key");
        emitter.LastEvent.Context["mutator_added_key"].Should().Be("mutator_value");
    }

    // ==================== Feature Name Extraction Tests ====================
    public sealed class OuterClass
    {
        public sealed class NestedMutationFeature : IMutationFeature<TestRequest, TestResult>
        {
            public IFeatureValidator<TestRequest> Validator { get; set; } = NoOpValidator<TestRequest>.Instance;
            public IFeatureRequirements<TestRequest> Requirements { get; set; } = NoOpRequirements<TestRequest>.Instance;
            public IFeatureMutator<TestRequest, TestResult> Mutator { get; set; } = new TestMutator();
            public IFeatureSideEffects<TestRequest> SideEffects { get; set; } = NoOpSideEffects<TestRequest>.Instance;
        }
    }

    [Fact]
    public async Task WideEvent_NestedFeatureClass_ShouldExtractDeclaringTypeName()
    {
        // Arrange
        var feature = new OuterClass.NestedMutationFeature();
        var request = new TestRequest("test");
        var emitter = new CaptureWideEventEmitter();

        // Act
        await feature.ExecuteAsync(request, emitter);

        // Assert
        emitter.LastEvent.Should().NotBeNull();

        // Should use the declaring type name (OuterClass) rather than the nested type name
        emitter.LastEvent!.Feature!.FeatureName.Should().Be("OuterClass");
    }

    // Concurrency Tests
    private const int ConcurrencyCount = 100;

    [Fact]
    public async Task ConcurrentQueryExecution_ShouldHaveIsolatedContexts()
    {
        // Arrange
        var results = new System.Collections.Concurrent.ConcurrentBag<(int Index, VsaResult<TestResult> Result)>();

        // Act
        var tasks = Enumerable.Range(0, ConcurrencyCount)
            .Select(async i =>
            {
                var feature = new TestQueryFeature();
                var request = new TestRequest($"request_{i}");
                var result = await feature.ExecuteAsync(request);
                results.Add((i, result));
            });

        await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(ConcurrencyCount);
        results.All(r => !r.Result.IsError).Should().BeTrue();

        // Each result should have the correct request value
        foreach (var (index, result) in results)
        {
            result.Value.ProcessedValue.Should().Be($"Queried: request_{index}");
        }
    }

    [Fact]
    public async Task ConcurrentMutationExecution_ShouldHaveIsolatedContexts()
    {
        // Arrange
        var results = new System.Collections.Concurrent.ConcurrentBag<(int Index, VsaResult<TestResult> Result)>();

        // Act
        var tasks = Enumerable.Range(0, ConcurrencyCount)
            .Select(async i =>
            {
                var feature = new TestMutationFeature();
                var request = new TestRequest($"request_{i}");
                var result = await feature.ExecuteAsync(request);
                results.Add((i, result));
            });

        await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(ConcurrencyCount);
        results.All(r => !r.Result.IsError).Should().BeTrue();

        // Each result should have the correct request value
        foreach (var (index, result) in results)
        {
            result.Value.ProcessedValue.Should().Be($"Processed: request_{index}");
        }
    }

    [Fact]
    public async Task FeatureExecution_WithCancellation_DuringConcurrentOperations()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var completedCount = 0;
        var cancelledCount = 0;

        // Act - Start many tasks and cancel mid-way
        var tasks = Enumerable.Range(0, ConcurrencyCount)
            .Select(async i =>
            {
                try
                {
                    var feature = new TestMutationFeature { Validator = new CancelingValidator() };
                    var request = new TestRequest($"request_{i}");

                    // Cancel after first few complete
                    if (i == ConcurrencyCount / 2)
                    {
                        cts.Cancel();
                    }

                    await feature.ExecuteAsync(request, ct: cts.Token);
                    Interlocked.Increment(ref completedCount);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref cancelledCount);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

        await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty("no unexpected exceptions should occur");

        // Some tasks should have been cancelled
        cancelledCount.Should().BeGreaterThan(0, "some tasks should have been cancelled");
    }

    // Test that FeatureContext AddContext is safe when each execution has its own context
    public sealed class ContextAddingMutatorForConcurrency : IFeatureMutator<TestRequest, TestResult>
    {
        public Task<VsaResult<TestResult>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            // Add multiple context values - each execution has its own FeatureContext
            context.AddContext("unique_id", Guid.NewGuid().ToString());
            context.AddContext("request_value", context.Request.Value);
            context.AddContext("timestamp", DateTimeOffset.UtcNow.Ticks);

            return Task.FromResult<VsaResult<TestResult>>(new TestResult($"Processed: {context.Request.Value}"));
        }
    }

    [Fact]
    public async Task FeatureContext_AddContext_EachExecutionHasIsolatedContext()
    {
        // Arrange
        var emitters = new System.Collections.Concurrent.ConcurrentBag<CaptureWideEventEmitter>();

        // Act
        var tasks = Enumerable.Range(0, ConcurrencyCount)
            .Select(async i =>
            {
                var emitter = new CaptureWideEventEmitter();
                var feature = new TestMutationFeature { Mutator = new ContextAddingMutatorForConcurrency() };
                var request = new TestRequest($"request_{i}");
                await feature.ExecuteAsync(request, emitter);
                emitters.Add(emitter);
            });

        await Task.WhenAll(tasks);

        // Assert
        emitters.Should().HaveCount(ConcurrencyCount);

        var uniqueIds = emitters
            .Where(e => e.LastEvent != null)
            .Select(e => e.LastEvent!.Context["unique_id"])
            .ToList();

        // All unique IDs should be distinct - no context leakage
        uniqueIds.Distinct().Should().HaveCount(ConcurrencyCount);
    }

    public sealed class EntityAddingRequirements : IFeatureRequirements<TestRequest>
    {
        private readonly int _index;

        public EntityAddingRequirements(int index)
        {
            _index = index;
        }

        public Task<VsaResult<FeatureContext<TestRequest>>> EnforceAsync(TestRequest request, CancellationToken ct = default)
        {
            var context = new FeatureContext<TestRequest> { Request = request };
            context.SetEntity("index", _index);
            context.SetEntity("data", $"data_{_index}");
            return Task.FromResult<VsaResult<FeatureContext<TestRequest>>>(context);
        }
    }

    public sealed class EntityCapturingMutator : IFeatureMutator<TestRequest, TestResult>
    {
        public int? CapturedIndex { get; private set; }
        public string? CapturedData { get; private set; }

        public Task<VsaResult<TestResult>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            CapturedIndex = context.GetEntity<int>("index");
            CapturedData = context.GetEntity<string>("data");
            return Task.FromResult<VsaResult<TestResult>>(new TestResult($"Processed: {context.Request.Value}"));
        }
    }

    [Fact]
    public async Task FeatureContext_SetEntity_EachExecutionHasIsolatedEntities()
    {
        // Arrange
        var captures = new System.Collections.Concurrent.ConcurrentBag<(int ExpectedIndex, EntityCapturingMutator Mutator)>();

        // Act
        var tasks = Enumerable.Range(0, ConcurrencyCount)
            .Select(async i =>
            {
                var mutator = new EntityCapturingMutator();
                var feature = new TestMutationFeature
                {
                    Requirements = new EntityAddingRequirements(i),
                    Mutator = mutator,
                };
                var request = new TestRequest($"request_{i}");
                await feature.ExecuteAsync(request);
                captures.Add((i, mutator));
            });

        await Task.WhenAll(tasks);

        // Assert
        captures.Should().HaveCount(ConcurrencyCount);

        // Each mutator should have captured its own index - no entity leakage
        foreach (var (expectedIndex, mutator) in captures)
        {
            mutator.CapturedIndex.Should().Be(expectedIndex);
            mutator.CapturedData.Should().Be($"data_{expectedIndex}");
        }
    }
}
