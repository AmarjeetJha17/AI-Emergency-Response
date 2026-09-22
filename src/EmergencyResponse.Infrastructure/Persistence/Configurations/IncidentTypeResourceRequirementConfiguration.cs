using EmergencyResponse.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmergencyResponse.Infrastructure.Persistence.Configurations;

public class IncidentTypeResourceRequirementConfiguration
    : IEntityTypeConfiguration<IncidentTypeResourceRequirement>
{
    public void Configure(EntityTypeBuilder<IncidentTypeResourceRequirement> builder)
    {
        builder.ToTable("IncidentTypeResourceRequirements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MinimumCount)
            .IsRequired();

        builder.Property(x => x.PreferredCount)
            .IsRequired();

        builder.Property(x => x.IsMandatory)
            .IsRequired();

        builder.Property(x => x.PriorityOrder)
            .IsRequired();

        builder.HasIndex(x => new { x.IncidentTypeId, x.ResourceType })
            .IsUnique();

        builder.HasOne(x => x.IncidentType)
            .WithMany(x => x.ResourceRequirements)
            .HasForeignKey(x => x.IncidentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
