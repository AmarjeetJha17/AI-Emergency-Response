namespace EmergencyResponse.Api.Common;

public class ApiError
{
    public string Code { get; init; } = null!;

    public string Message { get; init; } = null!;

    public string? TraceId { get; init; }

    public IDictionary<string, string[]>? Details { get; init; }
}