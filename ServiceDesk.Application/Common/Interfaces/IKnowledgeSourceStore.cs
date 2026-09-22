using ServiceDesk.Application.DTOs.Knowledge;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IKnowledgeSourceStore
{
    Task<IReadOnlyList<KnowledgeDocumentDto>> GetApprovedSourceDocumentsAsync(CancellationToken cancellationToken = default);
    Task<Stream?> GetDocumentStreamAsync(string blobPath, CancellationToken cancellationToken = default);
    Task<KnowledgeDocumentDto> UploadDocumentAsync(string fileName, Stream contentStream, string contentType = "text/markdown", CancellationToken cancellationToken = default);
}
