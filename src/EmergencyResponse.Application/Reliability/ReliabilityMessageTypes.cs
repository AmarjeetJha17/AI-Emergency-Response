namespace EmergencyResponse.Application.Reliability;

public static class ReliabilityMessageTypes
{
    public const string IncidentAIAnalysisRequested = "incident-ai-analysis-requested.v1";
    public const string VisualAnalysisRequested = "visual-analysis-requested.v1";
    public const string DuplicateDetectionRequested = "duplicate-detection-requested.v1";
    public const string ExternalContextEnrichmentRequested = "external-context-enrichment-requested.v1";
}

public sealed record IncidentAIAnalysisRequested(Guid IncidentId, Guid JobId, string SchemaVersion);
public sealed record VisualAnalysisRequested(Guid IncidentId, Guid MediaId, Guid JobId, string SchemaVersion);
public sealed record DuplicateDetectionRequested(Guid IncidentId, Guid JobId, string SchemaVersion);
public sealed record ExternalContextEnrichmentRequested(Guid IncidentId, Guid JobId, string SchemaVersion);
