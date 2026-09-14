using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ConversationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<ConversationSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConversationSessions
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConversationSessions
            .Include(c => c.Messages)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ConversationSession session, CancellationToken cancellationToken = default)
    {
        await _dbContext.ConversationSessions.AddAsync(session, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ConversationSession session, CancellationToken cancellationToken = default)
    {
        _dbContext.ConversationSessions.Update(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
