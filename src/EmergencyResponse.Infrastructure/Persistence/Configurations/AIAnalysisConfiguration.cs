using EmergencyResponse.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public class AIAnalysisConfiguration
    : IEntityTypeConfiguration<AIAnalysis>
{
    public void Configure(EntityTypeBuilder<AIAnalysis> builder)
    {
        builder.ToTable("AIAnalyses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ModelName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ModelVersion)
            .HasMaxLength(100);

        builder.Property(x => x.PromptVersion)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.InputReference)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.OutputJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.Confidence)
            .HasPrecision(5, 4);

        builder.Property(x => x.HumanDecision)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.IncidentId);

        builder.HasIndex(x => x.CreatedAtUtc);

        builder.HasOne(x => x.Incident)
            .WithMany(x => x.AIAnalyses)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasMaxLength(2000);

        builder.Property(x => x.FailureCategory)
            .HasMaxLength(100);

        builder.Property(x => x.AttemptNumber)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(x => x.RetryCount)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.ExtractedFactsJson)
            .HasColumnType("nvarchar(max)");
    }
}