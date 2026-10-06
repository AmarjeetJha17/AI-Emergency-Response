using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using EmergencyResponse.Application.Knowledge;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Knowledge;

public sealed class KnowledgeSearchIndexBuilder
{
    private readonly KnowledgeSearchOptions _options;

    public KnowledgeSearchIndexBuilder(
        IOptions<KnowledgeSearchOptions> options)
    {
        _options = options.Value;
    }

    public async Task EnsureCreatedAsync(
        SearchIndexClient indexClient,
        int embeddingDimensions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            return;
        }

        var fields = new List<SearchField>
        {
            new(
                "id",
                SearchFieldDataType.String)
            {
                IsKey = true,
                IsFilterable = true
            },
            new("documentId", SearchFieldDataType.String)
            {
                IsFilterable = true
            },
            new("documentNumber", SearchFieldDataType.String)
            {
                IsSearchable = true,
                IsFilterable = true
            },
            new("title", SearchFieldDataType.String)
            {
                IsSearchable = true,
                IsFilterable = true
            },
            new("content", SearchFieldDataType.String)
            {
                IsSearchable = true
            },
            new("documentType", SearchFieldDataType.String)
            {
                IsFilterable = true
            },
            new("version", SearchFieldDataType.String)
            {
                IsFilterable = true
            },
            new("pageNumber", SearchFieldDataType.Int32)
            {
                IsFilterable = true
            },
            new("sectionHeading", SearchFieldDataType.String)
            {
                IsSearchable = true,
                IsFilterable = true
            },
            new("departmentId", SearchFieldDataType.String)
            {
                IsFilterable = true
            },
            new("isGlobal", SearchFieldDataType.Boolean)
            {
                IsFilterable = true
            },
            new("isActive", SearchFieldDataType.Boolean)
            {
                IsFilterable = true
            },
            new("allowedRoles", SearchFieldDataType.Collection(
                SearchFieldDataType.String))
            {
                IsFilterable = true
            },
            new("allowedDepartments", SearchFieldDataType.Collection(
                SearchFieldDataType.String))
            {
                IsFilterable = true
            },
            new("effectiveFrom", SearchFieldDataType.DateTimeOffset)
            {
                IsFilterable = true
            },
            new("effectiveUntil", SearchFieldDataType.DateTimeOffset)
            {
                IsFilterable = true
            },
            new("embedding", SearchFieldDataType.Collection(
                SearchFieldDataType.Single))
            {
                IsSearchable = true,
                VectorSearchDimensions = embeddingDimensions,
                VectorSearchProfileName = "knowledge-vector-profile"
            }
        };

        var index = new SearchIndex(_options.IndexName)
        {
            Fields = fields,
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("knowledge-hnsw")
                },
                Profiles =
                {
                    new VectorSearchProfile(
                        "knowledge-vector-profile",
                        "knowledge-hnsw")
                }
            }
        };

        await indexClient.CreateOrUpdateIndexAsync(
            index,
            cancellationToken: cancellationToken);
    }
}
