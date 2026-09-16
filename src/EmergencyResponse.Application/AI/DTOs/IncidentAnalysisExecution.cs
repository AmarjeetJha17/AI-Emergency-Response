namespace EmergencyResponse.Application.AI.DTOs;

public sealed record IncidentAnalysisExecution
{
    public IncidentAnalysisResult Result { get; init; } = null!;

    public string ModelName { get; init; } = null!;

    public string? ModelVersion { get; init; }

    public string PromptVersion { get; init; } = null!;

    public long ExecutionTimeMs { get; init; }
}