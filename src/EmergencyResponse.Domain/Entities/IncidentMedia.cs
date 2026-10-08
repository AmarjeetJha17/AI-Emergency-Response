using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public class IncidentMedia : BaseEntity
{
    public DataClassification DataClassification { get; set; } = DataClassification.Confidential;
    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public string StoragePath { get; set; } = null!;

    public bool IsProcessed { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public bool AIAnalysisRequested { get; set; }

    public DateTime? AIAnalysisCompletedAtUtc { get; set; }

    public string? ContentHash { get; set; }

    public string StorageKey { get; set; } = null!;

    public long SizeBytes { get; set; }
    public IncidentMediaType MediaType { get; set; }
}
