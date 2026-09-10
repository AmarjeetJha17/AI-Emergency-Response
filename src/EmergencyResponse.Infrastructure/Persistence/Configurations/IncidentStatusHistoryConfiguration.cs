using EmergencyResponse.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public class IncidentStatusHistoryConfiguration
    : IEntityTypeConfiguration<IncidentStatusHistory>
{
    public void Configure(EntityTypeBuilder<IncidentStatusHistory> builder)
    {
        builder.ToTable("IncidentStatusHistory");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChangedByUserId)
            .HasMaxLength(450);

        builder.Property(x => x.Remarks)
            .HasMaxLength(1000);

        builder.Property(x => x.ChangedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.IncidentId);

        builder.HasIndex(x => x.ChangedAtUtc);

        builder.HasOne(x => x.Incident)
            .WithMany(x => x.StatusHistory)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}