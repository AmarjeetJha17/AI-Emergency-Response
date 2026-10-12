using EmergencyResponse.Application.Reliability;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace EmergencyResponse.UnitTests.Reliability;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class ReliabilityFailureClassifierTests
{
    [Theory]
    [MemberData(nameof(FailureCases))]
    public void Categorizes_failure_by_retry_and_degradation_policy(
        Exception exception,
        string expected)
    {
        Assert.Equal(expected, ReliabilityFailureClassifier.Categorize(exception));
    }

    public static TheoryData<Exception, string> FailureCases => new()
    {
        { new AIServiceCircuitOpenException("circuit is open"), "DependencyUnavailable" },
        { new OperationCanceledException(), "Transient" },
        { new TimeoutException(), "Transient" },
        { new HttpRequestException(), "Transient" },
        { HttpFailure(HttpStatusCode.TooManyRequests), "RateLimited" },
        { HttpFailure(HttpStatusCode.Unauthorized), "Authentication" },
        { HttpFailure(HttpStatusCode.Forbidden), "Authentication" },
        { HttpFailure(HttpStatusCode.BadRequest), "Permanent" },
        { HttpFailure(HttpStatusCode.UnprocessableEntity), "Permanent" },
        { HttpFailure(HttpStatusCode.InternalServerError), "DependencyUnavailable" },
        { HttpFailure(HttpStatusCode.ServiceUnavailable), "DependencyUnavailable" },
        { new JsonException("invalid JSON"), "Validation" },
        { new ArgumentException("invalid argument"), "Validation" },
        { new DbUpdateConcurrencyException("concurrent update"), "Concurrency" },
        { new InvalidDataException("rejected content"), "ContentRejected" },
        { new Exception("unclassified"), "Unknown" }
    };

    private static HttpRequestException HttpFailure(HttpStatusCode statusCode)
        => new("HTTP dependency failure", inner: null, statusCode);
}
