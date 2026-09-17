using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IConversationRepository
{
    Task<ConversationSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationSession>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaginatedConversationHistoryDto> GetPagedSummariesAsync(
        Guid? userId,
        int page,
        int pageSize,
        string? search,
        string? timeFilter,
        CancellationToken cancellationToken = default);
    Task AddAsync(ConversationSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(ConversationSession session, CancellationToken cancellationToken = default);
}

