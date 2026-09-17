namespace ServiceDesk.Application.DTOs.Knowledge;

public record CitationDto(
    string DocumentName,
    string Section,
    int Page,
    string Snippet,
    string BlobPath
);
