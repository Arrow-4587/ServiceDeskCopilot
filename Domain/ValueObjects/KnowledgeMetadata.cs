using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Domain.ValueObjects;

public record KnowledgeMetadata
{
    public string DocumentName { get; init; }
    public string Version { get; init; }
    public string Section { get; init; }
    public int Page { get; init; }
    public bool Approved { get; init; }
    public bool Active { get; init; }
    public string BlobPath { get; init; }

    public KnowledgeMetadata(
        string documentName,
        string version,
        string section,
        int page,
        bool approved,
        bool active,
        string blobPath = "")
    {
        if (string.IsNullOrWhiteSpace(documentName))
            throw new DomainException("DocumentName cannot be empty.");

        DocumentName = documentName;
        Version = string.IsNullOrWhiteSpace(version) ? "1.0" : version;
        Section = section ?? string.Empty;
        Page = page > 0 ? page : 1;
        Approved = approved;
        Active = active;
        BlobPath = blobPath ?? string.Empty;
    }

    /// <summary>
    /// Grounding rule: Only approved AND active knowledge documents can be used to ground AI responses.
    /// </summary>
    public bool IsValidForGrounding => Approved && Active;
}
