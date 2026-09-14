using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IKnowledgeIndexStore
{
    Task UpsertChunkAsync(KnowledgeDocumentDto chunk, CancellationToken cancellationToken = default);
    Task UpsertChunksBatchAsync(IEnumerable<KnowledgeDocumentDto> chunks, CancellationToken cancellationToken = default);
    Task<int> GetIndexedCountAsync(CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
