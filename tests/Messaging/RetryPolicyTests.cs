using FluentAssertions;
using VsaResults;
using VsaResults.Messaging;
using Xunit;

namespace Tests.Messaging;

public class RetryPolicyTests
{
    private static readonly IReadOnlyList<Error> TestErrors = new List<Error>
    {
        Error.Failure("Test.Error", "Test error"),
    };

    [Fact]
    public void Immediate_ShouldRetryWithZeroDelay()
    {
        // Arrange
        var policy = RetryPolicy.Immediate(3);
        var context = new RetryContext { Attempt = 0 };

        // Act
        var shouldRetry = policy.ShouldRetry(context, TestErrors);
        var delay = policy.GetDelay(context);

        // Assert
        shouldRetry.Should().BeTrue();
        delay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Immediate_ShouldRespectMaxAttempts()
    {
        // Arrange - MaxRetries=3 means attempts 0, 1, 2 can retry, attempt 3 cannot
        var policy = RetryPolicy.Immediate(3);

        // Act & Assert
        policy.ShouldRetry(new RetryContext { Attempt = 0 }, TestErrors).Should().BeTrue();
        policy.ShouldRetry(new RetryContext { Attempt = 1 }, TestErrors).Should().BeTrue();
        policy.ShouldRetry(new RetryContext { Attempt = 2 }, TestErrors).Should().BeTrue();
        policy.ShouldRetry(new RetryContext { Attempt = 3 }, TestErrors).Should().BeFalse();
    }

    [Fact]
    public void Interval_ShouldRetryWithFixedDelay()
    {
        // Arrange
        var interval = TimeSpan.FromSeconds(5);
        var policy = RetryPolicy.Interval(3, interval);
        var context = new RetryContext { Attempt = 0 };

        // Act
        var delay = policy.GetDelay(context);

        // Assert
        delay.Should().Be(interval);
    }

    [Fact]
    public void Exponential_ShouldIncreaseDelayExponentially()
    {
        // Arrange - Exponential uses 2^Attempt formula
        var minInterval = TimeSpan.FromSeconds(1);
        var maxInterval = TimeSpan.FromSeconds(30);
        var policy = RetryPolicy.Exponential(5, minInterval, maxInterval);

        // Act - Attempt is 0-based
        var delay0 = policy.GetDelay(new RetryContext { Attempt = 0 });
        var delay1 = policy.GetDelay(new RetryContext { Attempt = 1 });
        var delay2 = policy.GetDelay(new RetryContext { Attempt = 2 });

        // Assert - 2^0 = 1, 2^1 = 2, 2^2 = 4
        delay0.Should().Be(TimeSpan.FromSeconds(1));
        delay1.Should().Be(TimeSpan.FromSeconds(2));
        delay2.Should().Be(TimeSpan.FromSeconds(4));
    }

    [Fact]
    public void Exponential_ShouldRespectMaxInterval()
    {
        // Arrange
        var minInterval = TimeSpan.FromSeconds(1);
        var maxInterval = TimeSpan.FromSeconds(10);
        var policy = RetryPolicy.Exponential(10, minInterval, maxInterval);

        // Act - attempt 4 would be 2^4 = 16 seconds, but should cap at 10
        var delay = policy.GetDelay(new RetryContext { Attempt = 4 });

        // Assert
        delay.Should().Be(maxInterval);
    }

    [Fact]
    public void None_ShouldNeverRetry()
    {
        // Arrange
        var policy = RetryPolicy.None;
        var context = new RetryContext { Attempt = 0 };

        // Act
        var shouldRetry = policy.ShouldRetry(context, TestErrors);

        // Assert
        shouldRetry.Should().BeFalse();
    }
}
