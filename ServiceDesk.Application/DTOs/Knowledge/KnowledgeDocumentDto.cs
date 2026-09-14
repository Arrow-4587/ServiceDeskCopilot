namespace ServiceDesk.Application.DTOs.Knowledge;

public record KnowledgeDocumentDto(
    string Id,
    string DocumentName,
    string Version,
    string Section,
    int Page,
    string Content,
    bool Approved,
    bool Active,
    string BlobPath,
    DateTime LastModified
);
