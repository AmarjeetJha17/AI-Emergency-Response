using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class IncidentTagConfiguration : IEntityTypeConfiguration<IncidentTag>
{
    public void Configure(EntityTypeBuilder<IncidentTag> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Tag).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AddedByUserId).HasMaxLength(450).IsRequired();
        builder.HasIndex(x => new { x.IncidentId, x.Tag }).IsUnique();
    }
}
