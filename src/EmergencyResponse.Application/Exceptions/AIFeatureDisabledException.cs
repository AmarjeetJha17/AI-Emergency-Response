namespace EmergencyResponse.Application.Exceptions;

public sealed class AIFeatureDisabledException : Exception
{
    public const string DefaultMessage =
        "This operation requires AI, and AI is disabled for this deployment.";

    public AIFeatureDisabledException()
        : base(DefaultMessage)
    {
    }
}
