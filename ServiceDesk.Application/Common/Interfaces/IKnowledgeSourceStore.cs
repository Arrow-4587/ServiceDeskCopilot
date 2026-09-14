using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IKnowledgeSourceStore
{
    Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedSourceDocumentsAsync(CancellationToken cancellationToken = default);
    Task<Stream?> GetDocumentStreamAsync(string blobPath, CancellationToken cancellationToken = default);
}
