namespace ServiceDesk.Application.DTOs.Knowledge;

public record SearchResultDto(
    string DocumentName,
    string Version,
    string Section,
    int Page,
    string Content,
    double Score,
    bool Approved,
    bool Active,
    string BlobPath
);
