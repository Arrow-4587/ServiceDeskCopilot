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
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IncidentDraft>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.IncidentDrafts
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(IncidentDraft draft, CancellationToken cancellationToken = default)
    {
        var local = _dbContext.IncidentDrafts.Local.FirstOrDefault(entry => entry.Id == draft.Id);
        if (local != null)
        {
            _dbContext.Entry(local).State = EntityState.Detached;
        }

        var exists = await _dbContext.IncidentDrafts.AnyAsync(d => d.Id == draft.Id, cancellationToken);
        if (exists)
        {
            _dbContext.IncidentDrafts.Update(draft);
        }
        else
        {
            await _dbContext.IncidentDrafts.AddAsync(draft, cancellationToken);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(IncidentDraft draft, CancellationToken cancellationToken = default)
    {
        var local = _dbContext.IncidentDrafts.Local.FirstOrDefault(entry => entry.Id == draft.Id);
        if (local != null)
        {
            _dbContext.Entry(local).State = EntityState.Detached;
        }

        var exists = await _dbContext.IncidentDrafts.AnyAsync(d => d.Id == draft.Id, cancellationToken);
        if (exists)
        {
            _dbContext.IncidentDrafts.Update(draft);
        }
        else
        {
            await _dbContext.IncidentDrafts.AddAsync(draft, cancellationToken);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
