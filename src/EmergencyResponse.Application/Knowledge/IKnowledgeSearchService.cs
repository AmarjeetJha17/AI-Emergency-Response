namespace EmergencyResponse.Application.Knowledge;

public interface IKnowledgeSearchService
{
    bool IsAvailable { get; }

    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        string question,
        KnowledgeAccessContext accessContext,
        CancellationToken cancellationToken = default);

    Task IndexAsync(
        KnowledgeDocumentIndexRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        IReadOnlyList<string> searchDocumentIds,
        CancellationToken cancellationToken = default);
}

public sealed record KnowledgeDocumentIndexRequest(
    string SearchDocumentId,
    string DocumentId,
    string DocumentNumber,
    string Title,
    string Content,
    string DocumentType,
    string Version,
    int PageNumber,
    string? SectionHeading,
    Guid? DepartmentId,
    bool IsGlobal,
    IReadOnlyList<string> AllowedRoles,
    IReadOnlyList<string> AllowedDepartments,
    DateTime? EffectiveFromUtc,
    DateTime? EffectiveUntilUtc,
    bool IsActive,
    ReadOnlyMemory<float> Embedding);
