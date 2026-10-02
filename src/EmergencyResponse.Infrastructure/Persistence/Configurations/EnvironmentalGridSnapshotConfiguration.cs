using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class EnvironmentalGridSnapshotConfiguration
    : IEntityTypeConfiguration<EnvironmentalGridSnapshot>
{
    public void Configure(EntityTypeBuilder<EnvironmentalGridSnapshot> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CellKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ContextHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.CellKey, x.ObservedAtUtc });
        builder.HasIndex(x => x.ContextHash);
    }
}
