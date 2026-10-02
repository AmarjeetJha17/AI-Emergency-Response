using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class PredictiveAnalysisConfiguration
    : IEntityTypeConfiguration<PredictiveAnalysis>
{
    public void Configure(
        EntityTypeBuilder<PredictiveAnalysis> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ModelName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ModelVersion)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DatasetVersion)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.FeatureSchemaVersion)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.FeatureSnapshotJson)
            .IsRequired();

        builder.Property(x => x.FeatureProvenanceJson)
            .IsRequired();

        builder.Property(x => x.Probability)
            .HasPrecision(5, 4);

        builder.Property(x => x.RiskScore)
            .HasPrecision(5, 2);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(2000);

        builder.HasIndex(x => new
        {
            x.IncidentId,
            x.PredictionType,
            x.GeneratedAtUtc
        });

        builder.HasOne<Incident>()
            .WithMany()
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
