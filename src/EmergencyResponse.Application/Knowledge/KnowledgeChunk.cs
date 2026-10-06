namespace EmergencyResponse.Application.Knowledge;

public sealed record KnowledgeChunk(
    int ChunkNumber,
    string Content,
    int PageNumber,
    string? SectionHeading,
    int TokenEstimate);
