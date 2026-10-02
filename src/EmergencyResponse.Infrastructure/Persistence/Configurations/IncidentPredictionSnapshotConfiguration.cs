using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class IncidentPredictionSnapshotConfiguration
    : IEntityTypeConfiguration<IncidentPredictionSnapshot>
{
    public void Configure(EntityTypeBuilder<IncidentPredictionSnapshot> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FeatureSnapshotJson)
            .IsRequired();

        builder.Property(x => x.FeatureHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.IncidentId,
            x.PredictionType,
            x.SnapshotAtUtc
        });

        builder.HasIndex(x => x.FeatureHash);

        builder.HasOne<Incident>()
            .WithMany()
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
