namespace ServiceDesk.Application.DTOs.Knowledge;

public record RagAnswerResultDto(
    string Answer,
    IReadOnlyList<CitationDto> Citations,
    bool Grounded,
    bool TriggeredFallback
);
