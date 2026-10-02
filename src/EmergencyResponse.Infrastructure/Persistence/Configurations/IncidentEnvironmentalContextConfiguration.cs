using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class IncidentEnvironmentalContextConfiguration
    : IEntityTypeConfiguration<IncidentEnvironmentalContext>
{
    public void Configure(EntityTypeBuilder<IncidentEnvironmentalContext> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProviderVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.WeatherPhrase).HasMaxLength(500);
        builder.Property(x => x.AirQualityCategory).HasMaxLength(100);
        builder.Property(x => x.ContextHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RawResponseJson);
        builder.HasIndex(x => new { x.IncidentId, x.ObservedAtUtc });
        builder.HasIndex(x => x.ContextHash);
        builder.HasOne<Incident>()
            .WithMany()
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
