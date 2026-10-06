namespace EmergencyResponse.Application.Copilot.Actions;

public sealed class CopilotActionOptions
{
    public const string SectionName = "Copilot:Actions";

    public int ProposalExpirationMinutes { get; init; } = 5;
}
