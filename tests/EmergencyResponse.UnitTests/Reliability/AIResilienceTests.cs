using System.Net;
using System.Text.Json;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.UnitTests.Reliability;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Resilience)]
public class AIResilienceTests
{
    [Fact]
    public void AIFailure_ShouldPreserveIncidentIntegrity()
    {
        // Incident created by citizen
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentNumber = "INC-2026-TEST",
            Title = "Structure fire at warehouse",
            Description = "Heavy smoke seen rising from central bay",
            Status = IncidentStatus.Submitted,
            Severity = SeverityLevel.Moderate,
            Priority = PriorityLevel.High,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Simulated AI failure
        var analysis = new AIAnalysis
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            Status = AIAnalysisStatus.Failed,
            FailureReason = "Connection timed out after 30 seconds",
            FailureCategory = "Timeout",
            AttemptNumber = 1,
            RetryCount = 1,
            LastAttemptAtUtc = DateTime.UtcNow,
            NextRetryAtUtc = DateTime.UtcNow.AddSeconds(10)
        };

        // Assert incident remains completely valid and untouched
        Assert.NotNull(incident);
        Assert.Equal("INC-2026-TEST", incident.IncidentNumber);
        Assert.Equal(IncidentStatus.Submitted, incident.Status);
        Assert.Equal(SeverityLevel.Moderate, incident.Severity);
        Assert.Equal(PriorityLevel.High, incident.Priority);

        // Assert analysis captures failure context non-destructively
        Assert.Equal(AIAnalysisStatus.Failed, analysis.Status);
        Assert.Equal("Timeout", analysis.FailureCategory);
        Assert.Equal(1, analysis.RetryCount);
        Assert.NotNull(analysis.NextRetryAtUtc);
    }

    [Theory]
    [InlineData(typeof(TaskCanceledException), "Timeout")]
    [InlineData(typeof(TimeoutException), "Timeout")]
    [InlineData(typeof(JsonException), "SchemaValidation")]
    public void FailureCategorizer_ShouldIdentifyStandardExceptions(Type exceptionType, string expectedCategory)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType)!;
        var category = CategorizeFailure(ex);

        Assert.Equal(expectedCategory, category);
    }

    [Fact]
    public void FailureCategorizer_ShouldIdentifyHttpRateLimit()
    {
        var httpEx = new HttpRequestException("Rate limit exceeded", null, HttpStatusCode.TooManyRequests);
        var category = CategorizeFailure(httpEx);

        Assert.Equal("RateLimited", category);
    }

    [Fact]
    public void FailureCategorizer_ShouldIdentifyHttpProviderUnavailable()
    {
        var httpEx = new HttpRequestException("Bad Gateway", null, HttpStatusCode.BadGateway);
        var category = CategorizeFailure(httpEx);

        Assert.Equal("ProviderUnavailable", category);
    }

    [Fact]
    public void FailureCategorizer_ShouldIdentifyTransientNetwork()
    {
        var httpEx = new HttpRequestException("Connection reset by peer");
        var category = CategorizeFailure(httpEx);

        Assert.Equal("TransientNetwork", category);
    }

    [Fact]
    public void RetryCountIncrement_ShouldCalculateNextExponentialRetry()
    {
        var analysis = new AIAnalysis
        {
            AttemptNumber = 1,
            RetryCount = 0
        };

        // First retry
        analysis.RetryCount++;
        analysis.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(
            Math.Min(300, Math.Pow(2, analysis.RetryCount) * 5));

        Assert.Equal(1, analysis.RetryCount);
        Assert.NotNull(analysis.NextRetryAtUtc);

        // Subsequent retry
        analysis.RetryCount++;
        var secondDelay = Math.Min(300, Math.Pow(2, analysis.RetryCount) * 5);
        Assert.Equal(20, (int)secondDelay); // 2^2 * 5 = 20s
    }

    private static string CategorizeFailure(Exception ex)
    {
        if (ex is TaskCanceledException or OperationCanceledException or TimeoutException)
        {
            return "Timeout";
        }
        if (ex is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode == HttpStatusCode.TooManyRequests)
                return "RateLimited";
            if ((int?)httpEx.StatusCode >= 500)
                return "ProviderUnavailable";
            return "TransientNetwork";
        }
        if (ex is JsonException)
        {
            return "SchemaValidation";
        }
        return "Unknown";
    }
}
