using System.Text.Json.Serialization;

namespace EmergencyResponse.Infrastructure.Knowledge;

public sealed class KnowledgeSearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    [JsonPropertyName("documentNumber")]
    public string DocumentNumber { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    [JsonPropertyName("sectionHeading")]
    public string? SectionHeading { get; set; }

    [JsonPropertyName("departmentId")]
    public string? DepartmentId { get; set; }

    [JsonPropertyName("isGlobal")]
    public bool IsGlobal { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("allowedRoles")]
    public IReadOnlyList<string> AllowedRoles { get; set; } = [];

    [JsonPropertyName("allowedDepartments")]
    public IReadOnlyList<string> AllowedDepartments { get; set; } = [];

    [JsonPropertyName("effectiveFrom")]
    public DateTime? EffectiveFromUtc { get; set; }

    [JsonPropertyName("effectiveUntil")]
    public DateTime? EffectiveUntilUtc { get; set; }

    [JsonPropertyName("embedding")]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
