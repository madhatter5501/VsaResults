using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VsaResults;
using VsaResults.WideEvents;

namespace Tests;

public class FeatureControllerTests
{
    public sealed record TestRequest(string Value);

    public sealed record TestResult(string ProcessedValue);

    // Test query feature
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

    // Test mutation feature
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
            return Task.FromResult<VsaResult<TestResult>>(new TestResult($"Mutated: {context.Request.Value}"));
        }
    }

    // Unit mutation feature for NoContent tests
    public sealed class TestUnitMutationFeature : IMutationFeature<TestRequest, Unit>
    {
        public IFeatureValidator<TestRequest> Validator { get; set; } = NoOpValidator<TestRequest>.Instance;

        public IFeatureRequirements<TestRequest> Requirements { get; set; } = NoOpRequirements<TestRequest>.Instance;

        public IFeatureMutator<TestRequest, Unit> Mutator { get; set; } = new TestUnitMutator();

        public IFeatureSideEffects<TestRequest> SideEffects { get; set; } = NoOpSideEffects<TestRequest>.Instance;
    }

    public sealed class TestUnitMutator : IFeatureMutator<TestRequest, Unit>
    {
        public Task<VsaResult<Unit>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            return Task.FromResult<VsaResult<Unit>>(Unit.Value);
        }
    }

    // Failing query feature
    public sealed class FailingQueryFeature : IQueryFeature<TestRequest, TestResult>
    {
        public IFeatureValidator<TestRequest> Validator { get; set; } = NoOpValidator<TestRequest>.Instance;

        public IFeatureQuery<TestRequest, TestResult> Query { get; set; } = new FailingQuery();
    }

    public sealed class FailingQuery : IFeatureQuery<TestRequest, TestResult>
    {
        public Task<VsaResult<TestResult>> ExecuteAsync(FeatureContext<TestRequest> context, CancellationToken ct = default)
        {
            return Task.FromResult<VsaResult<TestResult>>(Error.NotFound("Test.NotFound", "Entity not found"));
        }
    }

    // Test controller implementation
    private sealed class TestController : FeatureController
    {
        public Task<ActionResult<TestResult>> TestQueryOk(TestRequest request)
            => QueryOk<TestRequest, TestResult>(request);

        public Task<ActionResult<TestResult>> TestMutationOk(TestRequest request)
            => MutationOk<TestRequest, TestResult>(request);

        public Task<ActionResult<TestResult>> TestMutationCreated(TestRequest request)
            => MutationCreated<TestRequest, TestResult>(request, r => $"/test/{r.ProcessedValue}");

        public Task<IActionResult> TestMutationNoContent(TestRequest request)
            => MutationNoContent<TestRequest>(request);

        public Task<ActionResult<TestResult>> TestQueryCustom(TestRequest request)
            => Query<TestRequest, TestResult, ActionResult<TestResult>>(request, r =>
                r.Match<ActionResult<TestResult>>(
                    value => new OkObjectResult(value),
                    errors => new NotFoundResult()));
    }

    [Fact]
    public async Task QueryOk_WhenFeatureSucceeds_ReturnsOkResult()
    {
        // Arrange
        var services = new MockServiceProvider();
        services.AddService<IQueryFeature<TestRequest, TestResult>>(new TestQueryFeature());
        var controller = CreateController(services);

        // Act
        var result = await controller.TestQueryOk(new TestRequest("test"));

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.Value.Should().BeOfType<TestResult>();
        ((TestResult)okResult.Value!).ProcessedValue.Should().Be("Queried: test");
    }

    [Fact]
    public async Task QueryOk_WhenFeatureFails_ReturnsProblemDetails()
    {
        // Arrange
        var services = new MockServiceProvider();
        services.AddService<IQueryFeature<TestRequest, TestResult>>(new FailingQueryFeature());
        var controller = CreateController(services);

        // Act
        var result = await controller.TestQueryOk(new TestRequest("test"));

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task QueryOk_WhenFeatureNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new MockServiceProvider();
        var controller = CreateController(services);

        // Act
        var act = () => controller.TestQueryOk(new TestRequest("test"));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IQueryFeature*has been registered*");
    }

    [Fact]
    public async Task MutationOk_WhenFeatureSucceeds_ReturnsOkResult()
    {
        // Arrange
        var services = new MockServiceProvider();
        services.AddService<IMutationFeature<TestRequest, TestResult>>(new TestMutationFeature());
        var controller = CreateController(services);

        // Act
        var result = await controller.TestMutationOk(new TestRequest("test"));

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        ((TestResult)okResult.Value!).ProcessedValue.Should().Be("Mutated: test");
    }

    [Fact]
    public async Task MutationCreated_WhenFeatureSucceeds_ReturnsCreatedResult()
    {
        // Arrange
        var services = new MockServiceProvider();
        services.AddService<IMutationFeature<TestRequest, TestResult>>(new TestMutationFeature());
        var controller = CreateController(services);

        // Act
        var result = await controller.TestMutationCreated(new TestRequest("test"));

        // Assert
        result.Result.Should().BeOfType<CreatedResult>();
        var createdResult = (CreatedResult)result.Result!;
        createdResult.Location.Should().Be("/test/Mutated: test");
        ((TestResult)createdResult.Value!).ProcessedValue.Should().Be("Mutated: test");
    }

    [Fact]
    public async Task MutationNoContent_WhenFeatureSucceeds_ReturnsNoContentResult()
    {
        // Arrange
        var services = new MockServiceProvider();
        services.AddService<IMutationFeature<TestRequest, Unit>>(new TestUnitMutationFeature());
        var controller = CreateController(services);

        // Act
        var result = await controller.TestMutationNoContent(new TestRequest("test"));

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Query_WithCustomMapper_UsesMapperFunction()
    {
        // Arrange
        var services = new MockServiceProvider();
        services.AddService<IQueryFeature<TestRequest, TestResult>>(new FailingQueryFeature());
        var controller = CreateController(services);

        // Act
        var result = await controller.TestQueryCustom(new TestRequest("test"));

        // Assert - custom mapper returns NotFoundResult instead of ProblemDetails
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // Mock service provider
    private sealed class MockServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public void AddService<T>(T service)
            where T : notnull
        {
            _services[typeof(T)] = service;
        }

        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var service) ? service : null;
        }
    }

    private static TestController CreateController(MockServiceProvider serviceProvider)
    {
        serviceProvider.AddService<IWideEventEmitter>(NullWideEventEmitter.Instance);
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var controller = new TestController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return controller;
    }
}
