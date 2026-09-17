using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public class ConversationSessionConfiguration : IEntityTypeConfiguration<ConversationSession>
{
    public void Configure(EntityTypeBuilder<ConversationSession> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.UserId).IsRequired();
        builder.HasIndex(c => c.UserId);
        builder.HasMany(c => c.Messages)
               .WithOne()
               .HasForeignKey(m => m.ConversationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
