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
            .AsNoTracking()
            .Include(c => c.Messages)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSession>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConversationSessions
            .AsNoTracking()
            .Include(c => c.Messages)
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
        var localSession = _dbContext.ConversationSessions.Local.FirstOrDefault(s => s.Id == session.Id);
        if (localSession == null)
        {
            var exists = await _dbContext.ConversationSessions.AnyAsync(s => s.Id == session.Id, cancellationToken);
            if (!exists)
            {
                await _dbContext.ConversationSessions.AddAsync(session, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
            _dbContext.ConversationSessions.Attach(session);
            _dbContext.Entry(session).State = EntityState.Modified;
        }

        var existingMessageIds = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == session.Id)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);
        var existingMessageIdSet = new HashSet<Guid>(existingMessageIds);

        foreach (var message in session.Messages)
        {
            if (!existingMessageIdSet.Contains(message.Id))
            {
                var tracked = _dbContext.Messages.Local.FirstOrDefault(m => m.Id == message.Id);
                if (tracked != null)
                {
                    _dbContext.Entry(tracked).State = EntityState.Added;
                }
                else
                {
                    _dbContext.Messages.Add(message);
                }
            }
            else
            {
                var tracked = _dbContext.Messages.Local.FirstOrDefault(m => m.Id == message.Id);
                if (tracked == null)
                {
                    _dbContext.Messages.Attach(message);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
