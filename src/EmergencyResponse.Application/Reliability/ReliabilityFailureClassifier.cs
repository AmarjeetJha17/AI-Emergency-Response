namespace EmergencyResponse.Application.Reliability;

public static class ReliabilityFailureClassifier
{
    public static string Categorize(Exception exception)
    {
        if (exception is AIServiceCircuitOpenException) return "DependencyUnavailable";
        if (exception is OperationCanceledException or TimeoutException) return "Transient";
        if (exception is System.Net.Http.HttpRequestException http)
        {
            if (http.StatusCode == System.Net.HttpStatusCode.TooManyRequests) return "RateLimited";
            if (http.StatusCode == System.Net.HttpStatusCode.Unauthorized || http.StatusCode == System.Net.HttpStatusCode.Forbidden) return "Authentication";
            if (http.StatusCode is { } status && (int)status >= 500) return "DependencyUnavailable";
            if (http.StatusCode is { } clientStatus && (int)clientStatus >= 400) return "Permanent";
            return "Transient";
        }
        if (exception is System.Text.Json.JsonException or ArgumentException) return "Validation";
        if (exception.GetType().Name == "DbUpdateConcurrencyException") return "Concurrency";
        if (exception is InvalidDataException) return "ContentRejected";
        return "Unknown";
    }
}
