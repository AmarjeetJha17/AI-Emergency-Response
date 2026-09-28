namespace EmergencyResponse.Api.Common;

public class ApiResponse<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public string? Message { get; init; }

    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(
        T data,
        string? message = null,
        string? traceId = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            TraceId = traceId
        };
    }
}