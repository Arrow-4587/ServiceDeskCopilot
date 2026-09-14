using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Repositories;

public class IncidentDraftRepository : IIncidentDraftRepository
{
    private readonly ApplicationDbContext _dbContext;

    public IncidentDraftRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IncidentDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IncidentDrafts.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<IncidentDraft>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IncidentDrafts
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IncidentDraft>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.IncidentDrafts
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(IncidentDraft draft, CancellationToken cancellationToken = default)
    {
        await _dbContext.IncidentDrafts.AddAsync(draft, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(IncidentDraft draft, CancellationToken cancellationToken = default)
    {
        _dbContext.IncidentDrafts.Update(draft);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
