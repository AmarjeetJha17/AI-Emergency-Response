using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public sealed class KnowledgeDocumentChunk : BaseEntity
{
    public Guid KnowledgeDocumentId { get; set; }

    public int ChunkNumber { get; set; }

    public string Content { get; set; } = string.Empty;

    public int PageNumber { get; set; }

    public string? SectionHeading { get; set; }

    public string ChunkHash { get; set; } = string.Empty;

    public string SearchDocumentId { get; set; } = string.Empty;

    public int TokenEstimate { get; set; }

    public bool IsActive { get; set; }
}
