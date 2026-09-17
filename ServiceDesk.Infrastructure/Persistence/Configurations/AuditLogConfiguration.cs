using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.UserId).IsRequired().HasMaxLength(100);
        builder.Property(a => a.UserRole).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Details).IsRequired();
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.Timestamp).IsRequired();

        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.UserRole);
        builder.HasIndex(a => a.Action);
    }
}
