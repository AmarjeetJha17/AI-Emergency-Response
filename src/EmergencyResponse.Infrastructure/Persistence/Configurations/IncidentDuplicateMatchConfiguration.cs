using EmergencyResponse.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class IncidentDuplicateMatchConfiguration
    : IEntityTypeConfiguration<IncidentDuplicateMatch>
{
    public void Configure(
        EntityTypeBuilder<IncidentDuplicateMatch> builder)
    {
        builder.ToTable("IncidentDuplicateMatches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SemanticSimilarity)
            .HasPrecision(5, 4);

        builder.Property(x => x.GeographicSimilarity)
            .HasPrecision(5, 4);

        builder.Property(x => x.TemporalSimilarity)
            .HasPrecision(5, 4);

        builder.Property(x => x.IncidentTypeSimilarity)
            .HasPrecision(5, 4);

        builder.Property(x => x.DuplicateScore)
            .HasPrecision(5, 4);

        builder.Property(x => x.ReviewDecision)
            .HasMaxLength(100);

        builder.Property(x => x.ReviewedByUserId)
            .HasMaxLength(450);

        builder.HasIndex(x => new
            {
                x.SourceIncidentId,
                x.CandidateIncidentId
            })
            .IsUnique();

        builder.HasIndex(x => x.DuplicateScore);

        builder.HasOne(x => x.SourceIncident)
            .WithMany()
            .HasForeignKey(x => x.SourceIncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CandidateIncident)
            .WithMany()
            .HasForeignKey(x => x.CandidateIncidentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}