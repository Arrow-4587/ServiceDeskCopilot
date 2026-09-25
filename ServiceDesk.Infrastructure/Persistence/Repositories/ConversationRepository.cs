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
            .AsNoTracking()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConversationSessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Messages)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSession>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConversationSessions
            .AsNoTracking()
            .AsSplitQuery()
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

        if (totalCount == 0)
        {
            return new ServiceDesk.Application.DTOs.Chat.PaginatedConversationHistoryDto(
                new List<ServiceDesk.Application.DTOs.Chat.ConversationSummaryDto>(),
                page,
                pageSize,
                0,
                0);
        }

        var pagedSessions = await query
            .OrderByDescending(c => c.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.StartedAt,
                c.IsActive
            })
            .ToListAsync(cancellationToken);

        var sessionIds = pagedSessions.Select(s => s.Id).ToList();

        var messages = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => sessionIds.Contains(m.ConversationId))
            .Select(m => new
            {
                m.ConversationId,
                m.SenderRole,
                m.Content,
                m.Timestamp
            })
            .ToListAsync(cancellationToken);

        var messagesBySession = messages
            .GroupBy(m => m.ConversationId)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.Timestamp).ToList());

        var items = pagedSessions.Select(item =>
        {
            if (messagesBySession.TryGetValue(item.Id, out var msgs) && msgs.Count > 0)
            {
                var firstMessage = msgs[0].Content;
                var firstUserMessage = msgs.FirstOrDefault(m => string.Equals(m.SenderRole, "User", StringComparison.OrdinalIgnoreCase))?.Content;
                var lastMessageTime = msgs[^1].Timestamp;

                var titleText = !string.IsNullOrWhiteSpace(firstMessage) ? firstMessage : "New Conversation";
                var previewText = !string.IsNullOrWhiteSpace(firstUserMessage) ? firstUserMessage : titleText;

                return new ServiceDesk.Application.DTOs.Chat.ConversationSummaryDto(
                    item.Id,
                    titleText,
                    previewText,
                    lastMessageTime,
                    msgs.Count,
                    item.IsActive
                );
            }

            return new ServiceDesk.Application.DTOs.Chat.ConversationSummaryDto(
                item.Id,
                "New Conversation",
                "New Conversation",
                item.StartedAt,
                0,
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
