namespace EmergencyResponse.Application.Common.Configuration;

/// <summary>Engineering defaults only; organizations must set legal retention separately.</summary>
public sealed class RetentionOptions
{
    public const string SectionName = "Retention";

    public int CopilotConversationDays { get; set; } = 90;
    public int NotificationDays { get; set; } = 30;
    public int ApplicationLogDays { get; set; } = 30;
    public int FailedProcessingArtifactDays { get; set; } = 7;
    public int CleanupIntervalHours { get; set; } = 24;
}
