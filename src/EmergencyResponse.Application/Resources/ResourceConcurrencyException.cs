namespace EmergencyResponse.Application.Resources;

public sealed class ResourceConcurrencyException : Exception
{
    public ResourceConcurrencyException(string message)
        : base(message)
    {
    }

    public ResourceConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
