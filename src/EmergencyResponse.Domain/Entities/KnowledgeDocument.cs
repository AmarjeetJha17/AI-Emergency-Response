using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Knowledge;

namespace EmergencyResponse.Domain.Entities;

public sealed class KnowledgeDocument : BaseEntity
{
    public string DocumentNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public KnowledgeDocumentType DocumentType { get; set; }

    public KnowledgeDocumentStatus Status { get; set; }

    public string Version { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public DateTime? EffectiveFromUtc { get; set; }

    public DateTime? EffectiveUntilUtc { get; set; }

    public Guid? DepartmentId { get; set; }

    public bool IsGlobal { get; set; }

    public bool RequiresOperatorRole { get; set; }

    public string? AllowedRolesJson { get; set; }

    public string? AllowedDepartmentsJson { get; set; }

    public DateTime? IndexedAtUtc { get; set; }

    public string? IndexName { get; set; }

    public string? FailureReason { get; set; }
}
