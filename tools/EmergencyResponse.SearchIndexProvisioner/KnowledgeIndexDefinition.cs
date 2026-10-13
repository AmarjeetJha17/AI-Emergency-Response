using Azure.Search.Documents.Indexes.Models;

namespace EmergencyResponse.SearchIndexProvisioner;

/// <summary>
/// Deployment-time copy of the knowledge-index schema. Keep its fields and
/// vector profile aligned with Infrastructure/KnowledgeSearchIndexBuilder.
/// </summary>
internal static class KnowledgeIndexDefinition
{
    public static SearchIndex Create(string indexName, int embeddingDimensions)
    {
        var fields = new List<SearchField>
        {
            new("id", SearchFieldDataType.String)
            {
                IsKey = true,
                IsFilterable = true
            },
            new("documentId", SearchFieldDataType.String) { IsFilterable = true },
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
            new("content", SearchFieldDataType.String) { IsSearchable = true },
            new("documentType", SearchFieldDataType.String) { IsFilterable = true },
            new("version", SearchFieldDataType.String) { IsFilterable = true },
            new("pageNumber", SearchFieldDataType.Int32) { IsFilterable = true },
            new("sectionHeading", SearchFieldDataType.String)
            {
                IsSearchable = true,
                IsFilterable = true
            },
            new("departmentId", SearchFieldDataType.String) { IsFilterable = true },
            new("isGlobal", SearchFieldDataType.Boolean) { IsFilterable = true },
            new("isActive", SearchFieldDataType.Boolean) { IsFilterable = true },
            new("allowedRoles", SearchFieldDataType.Collection(SearchFieldDataType.String))
            {
                IsFilterable = true
            },
            new("allowedDepartments", SearchFieldDataType.Collection(SearchFieldDataType.String))
            {
                IsFilterable = true
            },
            new("effectiveFrom", SearchFieldDataType.DateTimeOffset) { IsFilterable = true },
            new("effectiveUntil", SearchFieldDataType.DateTimeOffset) { IsFilterable = true },
            new("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
                IsSearchable = true,
                VectorSearchDimensions = embeddingDimensions,
                VectorSearchProfileName = "knowledge-vector-profile"
            }
        };

        return new SearchIndex(indexName)
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
                    new VectorSearchProfile("knowledge-vector-profile", "knowledge-hnsw")
                }
            }
        };
    }
}
