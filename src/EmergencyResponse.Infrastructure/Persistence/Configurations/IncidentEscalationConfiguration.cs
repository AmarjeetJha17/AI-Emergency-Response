using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class IncidentEscalationConfiguration
    : IEntityTypeConfiguration<IncidentEscalation>
{
    public void Configure(
        EntityTypeBuilder<IncidentEscalation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Level)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.EvidenceJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Score)
            .HasPrecision(5, 2);

        builder.Property(x => x.AcknowledgedByUserId)
            .HasMaxLength(450);

        builder.Property(x => x.ResolvedByUserId)
            .HasMaxLength(450);

        builder.Property(x => x.ResolutionNotes)
            .HasMaxLength(2000);

        builder.HasOne(x => x.Incident)
            .WithMany()
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.IncidentId,
            x.Status,
            x.CreatedAtUtc
        });

        builder.HasIndex(x => new
        {
            x.Type,
            x.Status
        });
    }
}
