namespace EmergencyResponse.Application.Copilot;

public sealed class CopilotMemoryOptions
{
    public const string SectionName = "Copilot:Memory";

    public int RecentMessageCount { get; init; } = 12;

    public int MaxConversationMessages { get; init; } = 100;

    public int SummaryTriggerMessages { get; init; } = 20;

    public int ConversationRetentionDays { get; init; } = 90;

    public int AuditRetentionDays { get; init; } = 365;
}
