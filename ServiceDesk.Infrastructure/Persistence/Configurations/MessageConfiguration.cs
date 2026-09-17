using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        
        var valueComparer = new ValueComparer<List<Citation>>(
            (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions?)null),
            c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
            c => c == null ? new List<Citation>() : JsonSerializer.Deserialize<List<Citation>>(JsonSerializer.Serialize(c, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new List<Citation>()
        );

        builder.Property(m => m.Citations)
               .HasConversion(
                   v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                   v => JsonSerializer.Deserialize<List<Citation>>(v, (JsonSerializerOptions?)null) ?? new List<Citation>()
               )
               .Metadata.SetValueComparer(valueComparer);
    }
}
