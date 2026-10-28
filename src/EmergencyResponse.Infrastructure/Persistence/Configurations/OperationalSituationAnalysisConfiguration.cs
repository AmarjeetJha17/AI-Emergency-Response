using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class OperationalSituationAnalysisConfiguration
    : IEntityTypeConfiguration<OperationalSituationAnalysis>
{
    public void Configure(
        EntityTypeBuilder<OperationalSituationAnalysis> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PortfolioDemoOwnershipKey)
            .HasMaxLength(100);

        builder.Property(x => x.ModelName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PromptVersion)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.InputSnapshotJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.OutputJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.HumanDecision)
            .HasMaxLength(1000);

        builder.HasOne(x => x.OperationalSituation)
            .WithMany(x => x.Analyses)
            .HasForeignKey(x => x.OperationalSituationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.OperationalSituationId,
            x.GeneratedAtUtc
        });
    }
}
