using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public class IncidentDraftConfiguration : IEntityTypeConfiguration<IncidentDraft>
{
    public void Configure(EntityTypeBuilder<IncidentDraft> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.UserId).IsRequired();
        builder.HasIndex(i => i.UserId);

        builder.Property(i => i.Title).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Description).IsRequired();
        builder.Property(i => i.Category).HasMaxLength(100);

        builder.Property(i => i.Impact).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Urgency).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.ComputedPriority).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.ApprovalStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(i => i.SubmittedIncidentId).HasMaxLength(100);
    }
}
