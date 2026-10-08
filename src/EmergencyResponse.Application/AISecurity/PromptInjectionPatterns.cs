namespace EmergencyResponse.Application.AISecurity;

public static class PromptInjectionPatterns
{
    public static readonly string[] HighRiskPatterns =
    [
        "ignore previous instructions",
        "ignore all previous instructions",
        "ignore all ai instructions",
        "disregard system prompt",
        "reveal system prompt",
        "show hidden instructions",
        "developer message",
        "system message",
        "bypass safety",
        "disable safeguards",
        "execute this command",
        "call this tool",
        "approve this action",
        "send this data",
        "reveal secrets"
    ];
}
