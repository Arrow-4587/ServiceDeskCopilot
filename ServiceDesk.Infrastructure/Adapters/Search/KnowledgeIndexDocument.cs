using System.Text.Json.Serialization;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace ServiceDesk.Infrastructure.Adapters.Search;

public class KnowledgeIndexDocument
{
    [SimpleField(IsKey = true, IsFilterable = true)]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
    [JsonPropertyName("documentName")]
    public string DocumentName { get; set; } = string.Empty;

    [SearchableField(IsFilterable = true)]
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [SearchableField(IsFilterable = true, IsSortable = true)]
    [JsonPropertyName("section")]
    public string Section { get; set; } = string.Empty;

    [SimpleField(IsFilterable = true, IsSortable = true)]
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.EnLucene)]
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [SimpleField(IsFilterable = true)]
    [JsonPropertyName("approved")]
    public bool Approved { get; set; }

    [SimpleField(IsFilterable = true)]
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [SimpleField(IsFilterable = true)]
    [JsonPropertyName("blobPath")]
    public string BlobPath { get; set; } = string.Empty;

    [SimpleField(IsFilterable = true, IsSortable = true)]
    [JsonPropertyName("lastModified")]
    public DateTimeOffset LastModified { get; set; }

    [VectorSearchField(VectorSearchDimensions = 1536, VectorSearchProfileName = "knowledge-vector-profile")]
    [JsonPropertyName("contentVector")]
    public float[]? ContentVector { get; set; }
}
