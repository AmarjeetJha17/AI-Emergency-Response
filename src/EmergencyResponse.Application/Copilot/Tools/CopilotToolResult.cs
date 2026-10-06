namespace EmergencyResponse.Application.Copilot.Tools;

public sealed class CopilotToolResult<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTime RetrievedAtUtc { get; init; } = DateTime.UtcNow;

    public bool DataAvailable { get; init; }

    public static CopilotToolResult<T> Available(T data) =>
        new()
        {
            Success = true,
            Data = data,
            DataAvailable = true,
            RetrievedAtUtc = DateTime.UtcNow
        };

    public static CopilotToolResult<T> Unavailable(
        string errorCode,
        string errorMessage) =>
        new()
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            DataAvailable = false,
            RetrievedAtUtc = DateTime.UtcNow
        };
}

public sealed class CopilotToolAuthorizationException : Exception
{
    public CopilotToolAuthorizationException()
        : base("The current operator is not authorized to use this operational data.")
    {
    }
}

public static class CopilotToolValidation
{
    public static Guid RequiredId(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{name} is required.", name);
        }

        return value;
    }

    public static int Take(int value, int maximum = 20) =>
        Math.Clamp(value <= 0 ? 10 : value, 1, maximum);
}
