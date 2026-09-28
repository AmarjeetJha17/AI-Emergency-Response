using EmergencyResponse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public sealed class OperationalSituationIncidentConfiguration
    : IEntityTypeConfiguration<OperationalSituationIncident>
{
    public void Configure(
        EntityTypeBuilder<OperationalSituationIncident> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.OperationalSituation)
            .WithMany(x => x.Incidents)
            .HasForeignKey(x => x.OperationalSituationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Incident)
            .WithMany()
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.OperationalSituationId,
            x.IncidentId
        }).IsUnique();

        builder.HasIndex(x => x.IncidentId);
    }
}
