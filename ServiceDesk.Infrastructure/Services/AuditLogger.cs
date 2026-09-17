using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Infrastructure.Persistence;

namespace ServiceDesk.Infrastructure.Services;

public class AuditLogger : IAuditLogger
{
    private readonly ApplicationDbContext _dbContext;

    public AuditLogger(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task LogActionAsync(string action, string userId, string userRole, string details, string ipAddress = "", CancellationToken cancellationToken = default)
    {
        var log = new AuditLog(action, userId, userRole, details, ipAddress);
        _dbContext.AuditLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task LogActionAsync(string action, string userId, string details, string ipAddress = "", CancellationToken cancellationToken = default)
    {
        return LogActionAsync(action, userId, "Unknown", details, ipAddress, cancellationToken);
    }
}
