using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public class IncidentMedia : BaseEntity
{
    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public string StoragePath { get; set; } = null!;

    public bool IsProcessed { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }
}