using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Knowledge;

namespace EmergencyResponse.Application.Knowledge;

public interface IKnowledgeDocumentService
{
    Task<KnowledgeDocument> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        long length,
        string title,
        KnowledgeDocumentType documentType,
        string version,
        string? description,
        Guid? departmentId,
        bool isGlobal,
        IReadOnlyList<string> allowedRoles,
        IReadOnlyList<string> allowedDepartments,
        DateTime? effectiveFromUtc,
        DateTime? effectiveUntilUtc,
        CancellationToken cancellationToken = default);

    Task ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task PublishAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task ArchiveAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
