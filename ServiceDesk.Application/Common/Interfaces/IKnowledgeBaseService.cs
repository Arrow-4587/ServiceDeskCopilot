using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IKnowledgeBaseService
{
    Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedDocumentsAsync(CancellationToken cancellationToken = default);
    Task<KnowledgeDocumentDetailDto?> GetDocumentDetailAsync(string id, CancellationToken cancellationToken = default);
    void InvalidateCache();
}
