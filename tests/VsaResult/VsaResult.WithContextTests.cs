using FluentAssertions;
using VsaResults;

namespace Tests;

public class WithContextTests
{
    [Fact]
    public void WithContext_ShouldAddKeyValuePair()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;

        // Act
        var result = errorOrInt.WithContext("user.id", "abc123");

        // Assert
        result.Value.Should().Be(42);
        result.Context.Should().ContainKey("user.id");
        result.Context["user.id"].Should().Be("abc123");
    }

    [Fact]
    public void WithContext_ShouldPreserveExistingContext()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;

        // Act
        var result = errorOrInt
            .WithContext("key1", "value1")
            .WithContext("key2", "value2");

        // Assert
        result.Context.Should().HaveCount(2);
        result.Context["key1"].Should().Be("value1");
        result.Context["key2"].Should().Be("value2");
    }

    [Fact]
    public async Task WithContext_ShouldBeThreadSafe()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;

        // Act
        var tasks = Enumerable.Range(0, 20)
            .Select(index => Task.Run(() => errorOrInt.WithContext("thread", index)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        errorOrInt.Context.Should().BeEmpty();
        var values = results.Select(result => (int)result.Context["thread"]).ToList();
        values.Should().OnlyHaveUniqueItems().And.HaveCount(20);
    }

    [Fact]
    public void WithContext_WhenError_ShouldStillAddContext()
    {
        // Arrange
        VsaResult<int> errorOrInt = Error.NotFound();

        // Act
        var result = errorOrInt.WithContext("operation", "GetUser");

        // Assert
        result.IsError.Should().BeTrue();

        result.Context.Should().ContainKey("operation");
        result.Context["operation"].Should().Be("GetUser");
    }

    [Fact]
    public void WithContext_WithMultiplePairs_ShouldAddAll()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;

        // Act
        var result = errorOrInt.WithContext(
            ("user.id", "abc"),
            ("tenant.id", "xyz"),
            ("operation", "process"));

        // Assert
        result.Context.Should().HaveCount(3);
        result.Context["user.id"].Should().Be("abc");
        result.Context["tenant.id"].Should().Be("xyz");
        result.Context["operation"].Should().Be("process");
    }

    [Fact]
    public void WithContext_WithSelector_WhenSuccess_ShouldExtractFromValue()
    {
        // Arrange
        VsaResult<string> errorOrString = "hello@example.com";

        // Act
        var result = errorOrString.WithContext(email => ("domain", email.Split('@')[1]));

        // Assert
        result.Context["domain"].Should().Be("example.com");
    }

    [Fact]
    public void WithContext_WithSelector_WhenError_ShouldNotExecuteSelector()
    {
        // Arrange
        VsaResult<string> errorOrString = Error.NotFound();
        var selectorCalled = false;

        // Act
        var result = errorOrString.WithContext(_ =>
        {
            selectorCalled = true;
            return ("key", "value");
        });

        // Assert
        selectorCalled.Should().BeFalse();
        result.Context.Should().BeEmpty();
    }

    [Fact]
    public void WithErrorContext_WhenError_ShouldExtractFromErrors()
    {
        // Arrange
        VsaResult<int> errorOrInt = new List<Error>
        {
            Error.Validation(code: "Field.Required"),
            Error.Validation(code: "Field.TooShort"),
        };

        // Act
        var result = errorOrInt.WithErrorContext(errors => ("error.count", errors.Count));

        // Assert
        result.Context["error.count"].Should().Be(2);
    }

    [Fact]
    public void WithErrorContext_WhenSuccess_ShouldNotExecuteSelector()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;
        var selectorCalled = false;

        // Act
        var result = errorOrInt.WithErrorContext(_ =>
        {
            selectorCalled = true;
            return ("key", "value");
        });

        // Assert
        selectorCalled.Should().BeFalse();
        result.Context.Should().BeEmpty();
    }

    [Fact]
    public void WithFirstErrorContext_WhenError_ShouldExtractFromFirstError()
    {
        // Arrange
        VsaResult<int> errorOrInt = new List<Error>
        {
            Error.NotFound(code: "User.NotFound"),
            Error.Validation(code: "Other.Error"),
        };

        // Act
        var result = errorOrInt.WithFirstErrorContext(error => ("first.error.code", error.Code));

        // Assert
        result.Context["first.error.code"].Should().Be("User.NotFound");
    }

    [Fact]
    public void Context_ShouldPropagateThrough_Then()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;

        // Act
        var result = errorOrInt
            .WithContext("original", "context")
            .Then(x => x * 2);

        // Assert
        result.Value.Should().Be(84);
        result.Context["original"].Should().Be("context");
    }

    [Fact]
    public void Context_ShouldPropagateThrough_ThenWithErrorOr()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;

        // Act
        var result = errorOrInt
            .WithContext("before", "then")
            .Then(x => (VsaResult<string>)x.ToString())
            .WithContext("after", "then");

        // Assert
        result.Value.Should().Be("42");
        result.Context["before"].Should().Be("then");
        result.Context["after"].Should().Be("then");
    }

    [Fact]
    public void Context_ShouldPropagateOnError()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;

        // Act
        var result = errorOrInt
            .WithContext("before.error", "value")
            .Then<int>(_ => Error.NotFound());

        // Assert
        result.IsError.Should().BeTrue();
        result.Context["before.error"].Should().Be("value");
    }

    [Fact]
    public void Context_WhenOverwriting_NewerValueWins()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;

        // Act
        var result = errorOrInt
            .WithContext("key", "old")
            .WithContext("key", "new");

        // Assert
        result.Context["key"].Should().Be("new");
    }

    [Fact]
    public async Task WithContextAsync_ShouldWork()
    {
        // Arrange
        Task<VsaResult<int>> taskErrorOr = Task.FromResult<VsaResult<int>>(42);

        // Act
        var result = await taskErrorOr.WithContext("async.key", "async.value");

        // Assert
        result.Context["async.key"].Should().Be("async.value");
    }

    [Fact]
    public async Task WithContextAsync_WithSelector_ShouldWork()
    {
        // Arrange
        Task<VsaResult<int>> taskErrorOr = Task.FromResult<VsaResult<int>>(42);

        // Act
        var result = await taskErrorOr.WithContext(val => ("doubled", val * 2));

        // Assert
        result.Context["doubled"].Should().Be(84);
    }

    [Fact]
    public async Task Context_ShouldPropagateThrough_ThenAsync()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;

        // Act
        var result = await errorOrInt
            .WithContext("pre.async", "value")
            .ThenAsync(async x =>
            {
                await Task.Delay(1);
                return x * 2;
            });

        // Assert
        result.Value.Should().Be(84);
        result.Context["pre.async"].Should().Be("value");
    }

    [Fact]
    public void WithContext_ShouldBeImmutable()
    {
        // Arrange
        VsaResult<int> original = 42;

        // Act
        var modified = original.WithContext("key", "value");

        // Assert
        original.Context.Should().BeEmpty();
        modified.Context.Should().ContainKey("key");
    }

    [Fact]
    public void Context_DefaultShouldBeEmptyDictionary()
    {
        // Arrange
        VsaResult<int> errorOrInt = 42;

        // Assert
        errorOrInt.Context.Should().NotBeNull();
        errorOrInt.Context.Should().BeEmpty();
    }

    [Fact]
    public void WideEvent_EndToEndExample()
    {
        // Arrange & Act - Simulate a request flow
        var result = GetUserResult(userId: "user123")
            .WithContext("request.id", Guid.NewGuid().ToString())
            .WithContext("operation", "GetUserProfile")
            .WithContext(user => ("user.tier", user.Tier))
            .WithContext(user => ("user.name", user.Name))
            .Then(user => new { user.Name, OrderCount = 5 })
            .WithContext(profile => ("order.count", profile.OrderCount));

        // Assert - All context accumulated
        result.Context.Should().ContainKey("request.id");
        result.Context.Should().ContainKey("operation");
        result.Context.Should().ContainKey("user.tier");
        result.Context.Should().ContainKey("user.name");
        result.Context.Should().ContainKey("order.count");
        result.Context["operation"].Should().Be("GetUserProfile");
        result.Context["user.tier"].Should().Be("Premium");
        result.Context["order.count"].Should().Be(5);
    }

    private static VsaResult<User> GetUserResult(string userId)
    {
        return new User { Id = userId, Name = "Test User", Tier = "Premium" };
    }

    private record User
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Tier { get; init; } = string.Empty;
    }
}
