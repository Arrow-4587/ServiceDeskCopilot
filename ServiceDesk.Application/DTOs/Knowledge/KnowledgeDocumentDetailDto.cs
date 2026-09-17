namespace ServiceDesk.Application.DTOs.Knowledge;

public record KnowledgeDocumentDetailDto(
    KnowledgeDocumentDto Document,
    string RenderedHtml
);
