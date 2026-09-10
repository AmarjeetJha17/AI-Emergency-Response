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

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.StoragePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasIndex(x => x.IncidentId);

        builder.HasOne(x => x.Incident)
            .WithMany(x => x.Media)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}