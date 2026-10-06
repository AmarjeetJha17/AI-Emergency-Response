using System.Security.Cryptography;
using System.Text.Json;
using EmergencyResponse.Application.Knowledge;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Knowledge;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Knowledge;

public sealed class KnowledgeDocumentService : IKnowledgeDocumentService
{
    private readonly ApplicationDbContext _db;
    private readonly IKnowledgeDocumentStorage _storage;
    private readonly IKnowledgeDocumentNumberGenerator _numbers;
    private readonly IKnowledgeDocumentParser _parser;
    private readonly IKnowledgeDocumentChunker _chunker;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    private readonly IKnowledgeSearchService _search;
    private readonly KnowledgeDocumentOptions _options;
    private readonly ILogger<KnowledgeDocumentService> _logger;

    public KnowledgeDocumentService(
        ApplicationDbContext db,
        IKnowledgeDocumentStorage storage,
        IKnowledgeDocumentNumberGenerator numbers,
        IKnowledgeDocumentParser parser,
        IKnowledgeDocumentChunker chunker,
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        IKnowledgeSearchService search,
        IOptions<KnowledgeDocumentOptions> options,
        ILogger<KnowledgeDocumentService> logger)
    {
        _db = db;
        _storage = storage;
        _numbers = numbers;
        _parser = parser;
        _chunker = chunker;
        _embeddings = embeddings;
        _search = search;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<KnowledgeDocument> UploadAsync(
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
        CancellationToken cancellationToken = default)
    {
        KnowledgeDocumentValidation.Validate(
            fileName,
            contentType,
            length,
            _options.MaxFileSizeBytes);

        if (string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException(
                "Knowledge document title and version are required.");
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var hash = Convert.ToHexString(
            SHA256.HashData(buffer.GetBuffer().AsSpan(
                0,
                checked((int)buffer.Length))));
        buffer.Position = 0;

        var storageKey = await _storage.SaveAsync(
            buffer,
            fileName,
            contentType,
            cancellationToken);

        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            DocumentNumber = await _numbers.GenerateAsync(cancellationToken),
            Title = title.Trim(),
            DocumentType = documentType,
            Status = KnowledgeDocumentStatus.Draft,
            Version = version.Trim(),
            FileName = Path.GetFileName(fileName),
            ContentType = contentType,
            StorageKey = storageKey,
            SizeBytes = length,
            ContentHash = hash,
            Description = description,
            DepartmentId = departmentId,
            IsGlobal = isGlobal,
            RequiresOperatorRole = allowedRoles.Count > 0,
            AllowedRolesJson = JsonSerializer.Serialize(allowedRoles),
            AllowedDepartmentsJson = JsonSerializer.Serialize(allowedDepartments),
            EffectiveFromUtc = effectiveFromUtc,
            EffectiveUntilUtc = effectiveUntilUtc
        };

        _db.KnowledgeDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken);
        return document;
    }

    public async Task ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _db.KnowledgeDocuments
            .SingleOrDefaultAsync(x => x.Id == documentId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Knowledge document '{documentId}' was not found.");

        document.Status = KnowledgeDocumentStatus.Processing;
        document.FailureReason = null;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await using var content = await _storage.OpenReadAsync(
                document.StorageKey,
                cancellationToken);
            var pages = await _parser.ParseAsync(
                content,
                document.FileName,
                cancellationToken);
            var chunks = _chunker.Chunk(pages);

            var existing = await _db.KnowledgeDocumentChunks
                .Where(x => x.KnowledgeDocumentId == document.Id)
                .ToListAsync(cancellationToken);
            var oldSearchIds = existing
                .Select(x => x.SearchDocumentId)
                .ToList();
            _db.KnowledgeDocumentChunks.RemoveRange(existing);
            await _search.DeleteAsync(oldSearchIds, cancellationToken);

            var roles = DeserializeList(document.AllowedRolesJson);
            var departments = DeserializeList(document.AllowedDepartmentsJson);
            foreach (var chunk in chunks)
            {
                var embeddingResult = await _embeddings.GenerateAsync(
                    [chunk.Content],
                    cancellationToken: cancellationToken);
                if (embeddingResult.Count == 0)
                {
                    throw new InvalidOperationException(
                        "The embedding service returned no embedding.");
                }

                var chunkHash = Convert.ToHexString(SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(chunk.Content)));
                var searchId = $"{document.Id:N}-{chunk.ChunkNumber}";
                _db.KnowledgeDocumentChunks.Add(new KnowledgeDocumentChunk
                {
                    Id = Guid.NewGuid(),
                    KnowledgeDocumentId = document.Id,
                    ChunkNumber = chunk.ChunkNumber,
                    Content = chunk.Content,
                    PageNumber = chunk.PageNumber,
                    SectionHeading = chunk.SectionHeading,
                    ChunkHash = chunkHash,
                    SearchDocumentId = searchId,
                    TokenEstimate = chunk.TokenEstimate,
                    IsActive = true
                });

                await _search.IndexAsync(
                    new KnowledgeDocumentIndexRequest(
                        searchId,
                        document.Id.ToString(),
                        document.DocumentNumber,
                        document.Title,
                        chunk.Content,
                        document.DocumentType.ToString(),
                        document.Version,
                        chunk.PageNumber,
                        chunk.SectionHeading,
                        document.DepartmentId,
                        document.IsGlobal,
                        roles,
                        departments,
                        document.EffectiveFromUtc,
                        document.EffectiveUntilUtc,
                        false,
                        embeddingResult[0].Vector),
                    cancellationToken);
            }

            document.Status = KnowledgeDocumentStatus.Processing;
            document.IndexedAtUtc = DateTime.UtcNow;
            document.IndexName = "emergency-knowledge";
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            document.Status = KnowledgeDocumentStatus.Failed;
            document.FailureReason = ex.Message[..Math.Min(
                ex.Message.Length,
                2000)];
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Knowledge document {DocumentId} processing failed.",
                documentId);
            throw;
        }
    }

    public async Task PublishAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _db.KnowledgeDocuments
            .SingleOrDefaultAsync(x => x.Id == documentId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Knowledge document '{documentId}' was not found.");
        if (!await _db.KnowledgeDocumentChunks.AnyAsync(
                x => x.KnowledgeDocumentId == documentId && x.IsActive,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "A knowledge document must be processed before publishing.");
        }

        document.Status = KnowledgeDocumentStatus.Active;
        document.PublishedAtUtc ??= DateTime.UtcNow;
        var chunks = await _db.KnowledgeDocumentChunks
            .Where(x => x.KnowledgeDocumentId == document.Id && x.IsActive)
            .ToListAsync(cancellationToken);
        var roles = DeserializeList(document.AllowedRolesJson);
        var departments = DeserializeList(document.AllowedDepartmentsJson);
        foreach (var chunk in chunks)
        {
            var embedding = await _embeddings.GenerateAsync(
                [chunk.Content],
                cancellationToken: cancellationToken);
            if (embedding.Count == 0)
            {
                throw new InvalidOperationException(
                    "The embedding service returned no embedding.");
            }

            await _search.IndexAsync(
                new KnowledgeDocumentIndexRequest(
                    chunk.SearchDocumentId,
                    document.Id.ToString(),
                    document.DocumentNumber,
                    document.Title,
                    chunk.Content,
                    document.DocumentType.ToString(),
                    document.Version,
                    chunk.PageNumber,
                    chunk.SectionHeading,
                    document.DepartmentId,
                    document.IsGlobal,
                    roles,
                    departments,
                    document.EffectiveFromUtc,
                    document.EffectiveUntilUtc,
                    true,
                    embedding[0].Vector),
                cancellationToken);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _db.KnowledgeDocuments
            .SingleOrDefaultAsync(x => x.Id == documentId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Knowledge document '{documentId}' was not found.");
        document.Status = KnowledgeDocumentStatus.Archived;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<string[]>(json) ?? [];
    }
}
