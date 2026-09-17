using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IIncidentDraftRepository
{
    Task<IncidentDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IncidentDraft>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IncidentDraft>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(IncidentDraft draft, CancellationToken cancellationToken = default);
    Task UpdateAsync(IncidentDraft draft, CancellationToken cancellationToken = default);
}
