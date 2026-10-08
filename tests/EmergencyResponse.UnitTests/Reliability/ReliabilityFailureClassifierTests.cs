using EmergencyResponse.Application.Reliability;

namespace EmergencyResponse.UnitTests.Reliability;

public sealed class ReliabilityFailureClassifierTests
{
    [Theory]
    [InlineData(typeof(TimeoutException), "Transient")]
    [InlineData(typeof(ArgumentException), "Validation")]
    [InlineData(typeof(InvalidDataException), "ContentRejected")]
    public void Categorizes_non_retryable_and_transient_failures(Type type, string expected)
    {
        var exception = (Exception)Activator.CreateInstance(type)!;
        Assert.Equal(expected, ReliabilityFailureClassifier.Categorize(exception));
    }
}
