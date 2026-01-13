using System.Collections.Concurrent;

using FluentAssertions;

using VsaResults;

namespace Tests;

public class ConcurrencyTests
{
    private const int HighConcurrencyCount = 1000;

    // Core ErrorOr Thread-Safety Tests
    [Fact]
    public async Task Then_WhenCalledConcurrently_ShouldProduceCorrectResults()
    {
        // Arrange
        var results = new ConcurrentBag<ErrorOr<int>>();

        // Act
        var tasks = Enumerable.Range(0, HighConcurrencyCount)
            .Select(i => Task.Run(() =>
            {
                ErrorOr<int> result = i;
                var transformed = result.Then(x => x * 2);
                results.Add(transformed);
            }));

        await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(HighConcurrencyCount);
        results.All(r => !r.IsError).Should().BeTrue();

        var values = results.Select(r => r.Value).OrderBy(x => x).ToList();
        var expected = Enumerable.Range(0, HighConcurrencyCount).Select(x => x * 2).ToList();
        values.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task ThenAsync_WhenCalledConcurrently_ShouldProduceCorrectResults()
    {
        // Arrange
        var results = new ConcurrentBag<ErrorOr<int>>();

        // Act
        var tasks = Enumerable.Range(0, HighConcurrencyCount)
            .Select(async i =>
            {
                ErrorOr<int> result = i;
                var transformed = await result.ThenAsync(async x =>
                {
                    await Task.Yield();
                    return x * 2;
                });
                results.Add(transformed);
            });

        await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(HighConcurrencyCount);
        results.All(r => !r.IsError).Should().BeTrue();

        var values = results.Select(r => r.Value).OrderBy(x => x).ToList();
        var expected = Enumerable.Range(0, HighConcurrencyCount).Select(x => x * 2).ToList();
        values.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task FailIf_WhenCalledConcurrently_ShouldProduceCorrectResults()
    {
        // Arrange
        var results = new ConcurrentBag<ErrorOr<int>>();
        var error = Error.Validation("Test.TooHigh", "Value is too high");

        // Act
        var tasks = Enumerable.Range(0, HighConcurrencyCount)
            .Select(i => Task.Run(() =>
            {
                ErrorOr<int> result = i;
                var checked_ = result.FailIf(x => x >= 500, error);
                results.Add(checked_);
            }));

        await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(HighConcurrencyCount);

        var successes = results.Where(r => !r.IsError).ToList();
        var failures = results.Where(r => r.IsError).ToList();

        successes.Should().HaveCount(500); // 0-499 pass
        failures.Should().HaveCount(500);  // 500-999 fail

        successes.Select(r => r.Value).Should().BeEquivalentTo(Enumerable.Range(0, 500));
    }

    [Fact]
    public async Task ConcurrentChaining_ThenFailIfElse_ShouldBeThreadSafe()
    {
        // Arrange
        var results = new ConcurrentBag<ErrorOr<string>>();
        var error = Error.Validation("Test.Negative", "Value became negative");

        // Act
        var tasks = Enumerable.Range(0, HighConcurrencyCount)
            .Select(i => Task.Run(() =>
            {
                ErrorOr<int> result = i;
                var chained = result
                    .Then(x => x * 2)
                    .Then(x => x - 1000)
                    .FailIf(x => x < 0, error)
                    .Then(x => $"Result: {x}");

                results.Add(chained);
            }));

        await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(HighConcurrencyCount);

        // Values 0-499: (x*2 - 1000) < 0, so they fail
        // Values 500-999: (x*2 - 1000) >= 0, so they succeed
        var successes = results.Where(r => !r.IsError).ToList();
        var failures = results.Where(r => r.IsError).ToList();

        successes.Should().HaveCount(500);
        failures.Should().HaveCount(500);
    }

    // Context Propagation Under Concurrency Tests
    [Fact]
    public async Task WithContext_HighConcurrency_StressTest()
    {
        // Arrange
        var results = new ConcurrentBag<ErrorOr<int>>();

        // Act
        var tasks = Enumerable.Range(0, HighConcurrencyCount)
            .Select(i => Task.Run(() =>
            {
                ErrorOr<int> result = i;
                var withContext = result
                    .WithContext("index", i)
                    .WithContext("squared", i * i)
                    .WithContext("thread", Environment.CurrentManagedThreadId);

                results.Add(withContext);
            }));

        await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(HighConcurrencyCount);

        foreach (var result in results)
        {
            result.IsError.Should().BeFalse();
            result.Context.Should().ContainKey("index");
            result.Context.Should().ContainKey("squared");
            result.Context.Should().ContainKey("thread");

            var index = (int)result.Context["index"];
            var squared = (int)result.Context["squared"];
            squared.Should().Be(index * index);
        }
    }

    [Fact]
    public async Task ContextPropagation_ThroughConcurrentChains_ShouldBeIsolated()
    {
        // Arrange
        var results = new ConcurrentBag<ErrorOr<string>>();

        // Act
        var tasks = Enumerable.Range(0, HighConcurrencyCount)
            .Select(i => Task.Run(() =>
            {
                ErrorOr<int> result = i;
                var chained = result
                    .WithContext("original", i)
                    .Then(x => x * 2)
                    .WithContext("doubled", i * 2)
                    .Then(x => $"Value: {x}");

                results.Add(chained);
            }));

        await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(HighConcurrencyCount);

        foreach (var result in results)
        {
            result.IsError.Should().BeFalse();
            result.Context.Should().ContainKey("original");
            result.Context.Should().ContainKey("doubled");

            var original = (int)result.Context["original"];
            var doubled = (int)result.Context["doubled"];
            doubled.Should().Be(original * 2);
        }
    }

    [Fact]
    public async Task MergeContext_WhenCalledConcurrently_ShouldProduceCorrectResults()
    {
        // Arrange
        var baseContext = new Dictionary<string, object>
        {
            ["base_key"] = "base_value",
            ["shared_key"] = "original",
        };

        var results = new ConcurrentBag<ErrorOr<int>>();

        // Act
        var tasks = Enumerable.Range(0, HighConcurrencyCount)
            .Select(i => Task.Run(() =>
            {
                ErrorOr<int> result = i;
                var withBase = result.WithContext(baseContext.Select(kv => (kv.Key, kv.Value)).ToArray());
                var withOverride = withBase.WithContext("shared_key", $"override_{i}");
                var withUnique = withOverride.WithContext("unique", i);

                results.Add(withUnique);
            }));

        await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(HighConcurrencyCount);

        foreach (var result in results)
        {
            result.Context.Should().ContainKey("base_key");
            result.Context["base_key"].Should().Be("base_value");

            result.Context.Should().ContainKey("shared_key");
            var sharedValue = (string)result.Context["shared_key"];
            sharedValue.Should().StartWith("override_");

            result.Context.Should().ContainKey("unique");
            var unique = (int)result.Context["unique"];
            sharedValue.Should().Be($"override_{unique}");
        }
    }

    // Match/Switch Under Concurrency Tests
    [Fact]
    public async Task Match_WhenCalledConcurrently_ShouldProduceCorrectResults()
    {
        // Arrange
        var results = new ConcurrentBag<string>();
        var error = Error.Validation("Test.Error", "Test error");

        // Act
        var tasks = Enumerable.Range(0, HighConcurrencyCount)
            .Select(i => Task.Run(() =>
            {
                ErrorOr<int> result = i % 2 == 0 ? i : error;
                var matched = result.Match(
                    value => $"Success:{value}",
                    errors => $"Error:{errors[0].Code}");

                results.Add(matched);
            }));

        await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(HighConcurrencyCount);

        var successes = results.Where(r => r.StartsWith("Success:")).ToList();
        var failures = results.Where(r => r.StartsWith("Error:")).ToList();

        successes.Should().HaveCount(500);
        failures.Should().HaveCount(500);
        failures.Should().AllBe("Error:Test.Error");
    }

    [Fact]
    public async Task Switch_WhenCalledConcurrently_ShouldExecuteCorrectBranch()
    {
        // Arrange
        var successCount = 0;
        var errorCount = 0;
        var lockObj = new object();
        var error = Error.Validation("Test.Error", "Test error");

        // Act
        var tasks = Enumerable.Range(0, HighConcurrencyCount)
            .Select(i => Task.Run(() =>
            {
                ErrorOr<int> result = i % 2 == 0 ? i : error;
                result.Switch(
                    value =>
                    {
                        lock (lockObj)
                        {
                            successCount++;
                        }
                    },
                    errors =>
                    {
                        lock (lockObj)
                        {
                            errorCount++;
                        }
                    });
            }));

        await Task.WhenAll(tasks);

        // Assert
        successCount.Should().Be(500);
        errorCount.Should().Be(500);
    }
}
