using EmergencyResponse.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public class IncidentMediaConfiguration
    : IEntityTypeConfiguration<IncidentMedia>
{
    public void Configure(EntityTypeBuilder<IncidentMedia> builder)
    {
        builder.ToTable("IncidentMedia");

        builder.HasKey(x => x.Id);

        // ── Current active columns ────────────────────────────────────────

        builder.Property(x => x.StorageKey)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SizeBytes)
            .IsRequired();

        builder.Property(x => x.ContentHash)
            .HasMaxLength(100);

        builder.Property(x => x.MediaType)
            .IsRequired();

        builder.Property(x => x.AIAnalysisRequested)
            .IsRequired();

        builder.Property(x => x.AIAnalysisCompletedAtUtc);

        // ── Legacy columns kept for schema compatibility ───────────────────
        // These were required in earlier migrations. The service now sets them
        // to the StorageKey value so NOT NULL constraints are satisfied.
        // A future migration can make them nullable or drop them.

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.StoragePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.FileSizeBytes)
            .IsRequired();

        // ── Indexes & relations ───────────────────────────────────────────

        builder.HasIndex(x => x.IncidentId);

        builder.HasOne(x => x.Incident)
            .WithMany(x => x.Media)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
