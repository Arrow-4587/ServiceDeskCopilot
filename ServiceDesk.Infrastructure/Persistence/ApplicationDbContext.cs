using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ConversationSession> ConversationSessions => Set<ConversationSession>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<IncidentDraft> IncidentDrafts => Set<IncidentDraft>();
    public DbSet<SystemPromptOverride> SystemPromptOverrides => Set<SystemPromptOverride>();
    public DbSet<ToolAllowList> ToolAllowLists => Set<ToolAllowList>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AdminConfig> AdminConfigs => Set<AdminConfig>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
