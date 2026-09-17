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

    public async Task<ServiceDesk.Application.DTOs.Chat.PaginatedConversationHistoryDto> GetPagedSummariesAsync(
        Guid? userId,
        int page,
        int pageSize,
        string? search,
        string? timeFilter,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.ConversationSessions
            .AsNoTracking()
            .AsQueryable();

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            query = query.Where(c => c.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(c => c.Messages.Any(m => EF.Functions.Like(m.Content.ToLower(), $"%{searchLower}%")));
        }

        if (!string.IsNullOrWhiteSpace(timeFilter))
        {
            var now = DateTime.UtcNow;
            var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

            switch (timeFilter.ToLower().Trim())
            {
                case "today":
                    query = query.Where(c => c.StartedAt >= todayStart);
                    break;
                case "yesterday":
                    var yesterdayStart = todayStart.AddDays(-1);
                    query = query.Where(c => c.StartedAt >= yesterdayStart && c.StartedAt < todayStart);
                    break;
                case "week":
                case "7days":
                    var weekStart = todayStart.AddDays(-7);
                    query = query.Where(c => c.StartedAt >= weekStart);
                    break;
                case "older":
                    var olderStart = todayStart.AddDays(-7);
                    query = query.Where(c => c.StartedAt < olderStart);
                    break;
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var rawItems = await query
            .OrderByDescending(c => c.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.StartedAt,
                c.IsActive,
                MessageCount = c.Messages.Count,
                FirstMessage = c.Messages.OrderBy(m => m.Timestamp).Select(m => m.Content).FirstOrDefault(),
                FirstUserMessage = c.Messages.Where(m => m.SenderRole == "User").OrderBy(m => m.Timestamp).Select(m => m.Content).FirstOrDefault(),
                LastMessageTime = c.Messages.OrderByDescending(m => m.Timestamp).Select(m => (DateTime?)m.Timestamp).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var items = rawItems.Select(item =>
        {
            var titleText = !string.IsNullOrWhiteSpace(item.FirstMessage) ? item.FirstMessage : "New Conversation";
            var previewText = !string.IsNullOrWhiteSpace(item.FirstUserMessage) ? item.FirstUserMessage : titleText;
            var lastActivity = item.LastMessageTime ?? item.StartedAt;

            return new ServiceDesk.Application.DTOs.Chat.ConversationSummaryDto(
                item.Id,
                titleText,
                previewText,
                lastActivity,
                item.MessageCount,
                item.IsActive
            );
        }).ToList();

        return new ServiceDesk.Application.DTOs.Chat.PaginatedConversationHistoryDto(items, page, pageSize, totalCount, totalPages);
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
