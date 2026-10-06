using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using EmergencyResponse.Application.Knowledge;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Knowledge;

public sealed class KnowledgeSearchService : IKnowledgeSearchService
{
    private readonly KnowledgeSearchOptions _options;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    private readonly KnowledgeSearchIndexBuilder _indexBuilder;
    private readonly ILogger<KnowledgeSearchService> _logger;
    private readonly SearchIndexClient? _indexClient;
    private readonly SearchClient? _searchClient;

    public KnowledgeSearchService(
        IOptions<KnowledgeSearchOptions> options,
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        KnowledgeSearchIndexBuilder indexBuilder,
        ILogger<KnowledgeSearchService> logger)
    {
        _options = options.Value;
        _embeddings = embeddings;
        _indexBuilder = indexBuilder;
        _logger = logger;

        if (Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpoint))
        {
            var credential = new AzureCliCredential();
            _indexClient = new SearchIndexClient(endpoint, credential);
            _searchClient = new SearchClient(
                endpoint,
                _options.IndexName,
                credential);
        }
    }

    public bool IsAvailable => _searchClient is not null;

    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        string question,
        KnowledgeAccessContext accessContext,
        CancellationToken cancellationToken = default)
    {
        if (_searchClient is null || _indexClient is null)
        {
            return [];
        }

        var vector = await _embeddings.GenerateAsync(
            question,
            cancellationToken: cancellationToken);
        await _indexBuilder.EnsureCreatedAsync(
            _indexClient,
            vector.Vector.Length,
            cancellationToken);

        var options = new SearchOptions
        {
            Size = _options.TopK,
            Filter = BuildFilter(accessContext),
            VectorSearch = new()
        };
        options.VectorSearch.Queries.Add(new VectorizedQuery(
            vector.Vector.ToArray())
        {
            KNearestNeighborsCount = _options.TopK,
            Fields = { "embedding" }
        });

        var response = await _searchClient.SearchAsync<KnowledgeSearchDocument>(
            question,
            options,
            cancellationToken);
        var results = new List<KnowledgeSearchResult>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            if (result.Score is null ||
                result.Score < _options.MinimumRetrievalScore)
            {
                continue;
            }

            results.Add(new KnowledgeSearchResult
            {
                SearchDocumentId = result.Document.Id,
                DocumentNumber = result.Document.DocumentNumber,
                Title = result.Document.Title,
                Version = result.Document.Version,
                PageNumber = result.Document.PageNumber,
                SectionHeading = result.Document.SectionHeading,
                Content = result.Document.Content,
                Score = result.Score,
                DocumentType = result.Document.DocumentType
            });
        }

        return results.Take(_options.MaximumContextChunks).ToList();
    }

    public async Task IndexAsync(
        KnowledgeDocumentIndexRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_searchClient is null || _indexClient is null)
        {
            return;
        }

        await _indexBuilder.EnsureCreatedAsync(
            _indexClient,
            request.Embedding.Length,
            cancellationToken);

        await _searchClient.MergeOrUploadDocumentsAsync(
            [
                new KnowledgeSearchDocument
                {
                    Id = request.SearchDocumentId,
                    DocumentId = request.DocumentId,
                    DocumentNumber = request.DocumentNumber,
                    Title = request.Title,
                    Content = request.Content,
                    DocumentType = request.DocumentType,
                    Version = request.Version,
                    PageNumber = request.PageNumber,
                    SectionHeading = request.SectionHeading,
                    DepartmentId = request.DepartmentId?.ToString(),
                    IsGlobal = request.IsGlobal,
                    IsActive = request.IsActive,
                    AllowedRoles = request.AllowedRoles,
                    AllowedDepartments = request.AllowedDepartments,
                    EffectiveFromUtc = request.EffectiveFromUtc,
                    EffectiveUntilUtc = request.EffectiveUntilUtc,
                    Embedding = request.Embedding
                }
            ],
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(
        IReadOnlyList<string> searchDocumentIds,
        CancellationToken cancellationToken = default)
    {
        if (_searchClient is null || searchDocumentIds.Count == 0)
        {
            return;
        }

        var batch = IndexDocumentsBatch.Delete(
            searchDocumentIds.Select(id => new KnowledgeSearchDocument
            {
                Id = id
            }));
        await _searchClient.IndexDocumentsAsync(
            batch,
            cancellationToken: cancellationToken);
    }

    private static string BuildFilter(
        KnowledgeAccessContext accessContext)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        var roleFilters = accessContext.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => $"allowedRoles/any(r: r eq '{Escape(role)}')")
            .ToList();
        var departmentFilters = accessContext.DepartmentId.HasValue
            ? new List<string>
            {
                $"allowedDepartments/any(d: d eq '{accessContext.DepartmentId.Value}')"
            }
            : new List<string>();

        var access = new List<string>
        {
            "isGlobal eq true"
        };
        access.AddRange(roleFilters);
        access.AddRange(departmentFilters);

        return $"({string.Join(" or ", access)}) and " +
               "isActive eq true and " +
               "(effectiveFrom eq null or effectiveFrom le " + now + ") and " +
               "(effectiveUntil eq null or effectiveUntil gt " + now + ")";
    }

    private static string Escape(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);
}
