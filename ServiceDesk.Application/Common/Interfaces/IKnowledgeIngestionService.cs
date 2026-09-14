namespace ServiceDesk.Application.Common.Interfaces;

public interface IKnowledgeIngestionService
{
    Task<int> IngestApprovedKnowledgeDocumentsAsync(CancellationToken cancellationToken = default);
}
