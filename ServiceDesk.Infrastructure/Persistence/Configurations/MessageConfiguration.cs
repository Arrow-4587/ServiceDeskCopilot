using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.ValueObjects;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.SenderRole).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Content).IsRequired();
        
        builder.Property(m => m.Citations)
               .HasConversion(
                   v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                   v => JsonSerializer.Deserialize<List<Citation>>(v, (JsonSerializerOptions?)null) ?? new List<Citation>()
               );
    }
}
