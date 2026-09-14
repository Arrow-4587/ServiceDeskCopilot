namespace ServiceDesk.Application.DTOs.Knowledge;

public record SearchQueryDto(
    string QueryText,
    int TopK = 5,
    bool FilterApprovedOnly = true
);
