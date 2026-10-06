using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class IncidentNoteConfiguration : IEntityTypeConfiguration<IncidentNote>
{
    public void Configure(EntityTypeBuilder<IncidentNote> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.HasIndex(x => new { x.IncidentId, x.CreatedAtUtc });
    }
}
