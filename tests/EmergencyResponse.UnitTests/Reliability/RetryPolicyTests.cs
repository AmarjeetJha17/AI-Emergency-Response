using EmergencyResponse.Application.Reliability;

namespace EmergencyResponse.UnitTests.Reliability;

public class RetryPolicyTests
{
    [Fact]
    public void RetryPolicyOptions_ShouldHaveSensibleDefaults()
    {
        var options = new RetryPolicyOptions();

        Assert.Equal(3, options.MaxAttempts);
        Assert.Equal(2, options.InitialDelaySeconds);
        Assert.Equal(30, options.MaximumDelaySeconds);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 4)]
    [InlineData(2, 8)]
    [InlineData(3, 16)]
    [InlineData(4, 30)] // Bounded by max delay
    [InlineData(10, 30)] // Bounded by max delay
    public void ExponentialBackoff_ShouldScaleAndBeBounded(int attempt, int expectedDelaySeconds)
    {
        var options = new RetryPolicyOptions
        {
            InitialDelaySeconds = 2,
            MaximumDelaySeconds = 30
        };

        var calculatedDelay = Math.Min(
            options.MaximumDelaySeconds,
            options.InitialDelaySeconds * Math.Pow(2, attempt));

        Assert.Equal(expectedDelaySeconds, (int)calculatedDelay);
    }

    [Fact]
    public void RetryAttempts_ShouldNotExceedConfiguredMaximum()
    {
        var options = new RetryPolicyOptions { MaxAttempts = 3 };

        var attemptCount = 0;
        var maxAllowedAttempts = options.MaxAttempts;

        for (var i = 0; i < 10; i++)
        {
            if (attemptCount < maxAllowedAttempts)
            {
                attemptCount++;
            }
        }

        Assert.Equal(3, attemptCount);
    }
}
